using System.Globalization;
using System.Security.Claims;
using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Sentra.Application.Abstractions;
using Sentra.Application.Security;
using Sentra.Contracts.Admin;
using Sentra.Domain.Condominiums;
using Sentra.Domain.Residents;
using Sentra.Infrastructure.Persistence;

namespace Sentra.Api.Endpoints;

public static class ImportEndpoints
{
    private const long MaxImportBytes = 10 * 1024 * 1024;

    public static IEndpointRouteBuilder MapSentraImportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin/import");

        group.MapPost("/units", ImportUnitsAsync)
            .DisableAntiforgery()
            .RequireAuthorization(PermissionCatalog.UnitsManage);

        group.MapPost("/residents", ImportResidentsAsync)
            .DisableAntiforgery()
            .RequireAuthorization(PermissionCatalog.ResidentsManage);

        return endpoints;
    }

    private static async Task<IResult> ImportUnitsAsync(
        HttpRequest request,
        ClaimsPrincipal user,
        SentraDbContext db,
        CancellationToken ct)
    {
        if (!TryCondominium(user, out var condominiumId))
            return Results.Unauthorized();

        var rowsResult = await ReadRowsAsync(request, ct);
        if (rowsResult.Error is not null)
            return Results.BadRequest(new { error = rowsResult.Error });

        var rows = rowsResult.Rows!;
        var imported = 0;
        var skipped = 0;
        var errors = new List<string>();

        for (var index = 0; index < rows.Count; index++)
        {
            var rowNumber = index + 2;
            var row = rows[index];

            var identifier = Get(row, "unidade", "identificador", "identifier");
            var displayName = Get(row, "nome", "displayname", "exibicao");
            var blockName = Get(row, "bloco", "block");

            if (string.IsNullOrWhiteSpace(identifier))
            {
                errors.Add($"Linha {rowNumber}: unidade/identificador ausente.");
                skipped++;
                continue;
            }

            displayName = string.IsNullOrWhiteSpace(displayName) ? identifier : displayName;

            if (await db.Units.AnyAsync(x =>
                    x.CondominiumId == condominiumId &&
                    x.Identifier == identifier.Trim(), ct))
            {
                skipped++;
                continue;
            }

            Guid? blockId = null;
            if (!string.IsNullOrWhiteSpace(blockName))
            {
                var normalizedBlock = blockName.Trim();
                var block = await db.Blocks.SingleOrDefaultAsync(x =>
                    x.CondominiumId == condominiumId &&
                    x.Name == normalizedBlock, ct);

                if (block is null)
                {
                    block = new Block(condominiumId, normalizedBlock);
                    db.Blocks.Add(block);
                }
                blockId = block.Id;
            }

            try
            {
                db.Units.Add(new Unit(
                    condominiumId,
                    identifier.Trim(),
                    displayName!.Trim(),
                    blockId));
                await db.SaveChangesAsync(ct);
                imported++;
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or DbUpdateException)
            {
                db.ChangeTracker.Clear();
                errors.Add($"Linha {rowNumber}: {SafeMessage(exception)}");
                skipped++;
            }
        }

        return Results.Ok(new ImportAdminResponse(rows.Count, imported, skipped, errors));
    }

    private static async Task<IResult> ImportResidentsAsync(
        HttpRequest request,
        ClaimsPrincipal user,
        SentraDbContext db,
        IClock clock,
        CancellationToken ct)
    {
        if (!TryCondominium(user, out var condominiumId))
            return Results.Unauthorized();

        var rowsResult = await ReadRowsAsync(request, ct);
        if (rowsResult.Error is not null)
            return Results.BadRequest(new { error = rowsResult.Error });

        var rows = rowsResult.Rows!;
        var imported = 0;
        var skipped = 0;
        var errors = new List<string>();

        for (var index = 0; index < rows.Count; index++)
        {
            var rowNumber = index + 2;
            var row = rows[index];

            var name = Get(row, "nome", "morador", "name");
            var phoneRaw = Get(row, "telefone", "celular", "phone");
            var unitIdentifier = Get(row, "unidade", "apartamento", "unit");
            var roleRaw = Get(row, "vinculo", "papel", "role");

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(phoneRaw) ||
                string.IsNullOrWhiteSpace(unitIdentifier))
            {
                errors.Add($"Linha {rowNumber}: nome, telefone e unidade são obrigatórios.");
                skipped++;
                continue;
            }

            string phone;
            try
            {
                phone = ResidentPhone.NormalizeE164(phoneRaw);
            }
            catch (ArgumentException)
            {
                errors.Add($"Linha {rowNumber}: telefone inválido.");
                skipped++;
                continue;
            }

            if (await db.ResidentPhones.AnyAsync(x => x.E164 == phone, ct))
            {
                skipped++;
                continue;
            }

            var unit = await db.Units.SingleOrDefaultAsync(x =>
                x.CondominiumId == condominiumId &&
                x.Identifier == unitIdentifier.Trim(), ct);

            if (unit is null)
            {
                errors.Add($"Linha {rowNumber}: unidade '{unitIdentifier.Trim()}' não encontrada.");
                skipped++;
                continue;
            }

            var role = ParseRole(roleRaw);
            var resident = new Resident(name.Trim());
            var residentPhone = new ResidentPhone(resident.Id, phone);
            var link = new ResidentUnit(
                resident.Id,
                unit.Id,
                role,
                clock.UtcNow);

            db.Residents.Add(resident);
            db.ResidentPhones.Add(residentPhone);
            db.ResidentUnits.Add(link);

            try
            {
                await db.SaveChangesAsync(ct);
                imported++;
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear();
                errors.Add($"Linha {rowNumber}: conflito de dados.");
                skipped++;
            }
        }

        return Results.Ok(new ImportAdminResponse(rows.Count, imported, skipped, errors));
    }

    private static ResidentUnitRole ParseRole(string? raw)
        => raw?.Trim().ToLowerInvariant() switch
        {
            "proprietario" or "proprietário" or "owner" => ResidentUnitRole.Owner,
            "inquilino" or "locatario" or "locatário" or "tenant" => ResidentUnitRole.Tenant,
            "familia" or "família" or "family" => ResidentUnitRole.Family,
            _ => ResidentUnitRole.Other
        };

    private static async Task<(List<Dictionary<string,string?>>? Rows, string? Error)> ReadRowsAsync(
        HttpRequest request,
        CancellationToken ct)
    {
        if (!request.HasFormContentType)
            return (null, "Envie multipart/form-data.");

        var form = await request.ReadFormAsync(ct);
        if (form.Files.Count != 1)
            return (null, "Envie exatamente um arquivo CSV ou XLSX.");

        var file = form.Files[0];
        if (file.Length <= 0 || file.Length > MaxImportBytes)
            return (null, "Arquivo vazio ou maior que 10 MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        await using var stream = file.OpenReadStream();

        return extension switch
        {
            ".csv" => (await ReadCsvAsync(stream, ct), null),
            ".xlsx" => (ReadXlsx(stream), null),
            _ => (null, "Formato suportado: .csv ou .xlsx.")
        };
    }

    internal static async Task<List<Dictionary<string,string?>>> ReadCsvAsync(
        Stream stream,
        CancellationToken ct)
    {
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);

        var headerLine = await reader.ReadLineAsync(ct)
            ?? throw new InvalidDataException("CSV sem cabeçalho.");

        var separator = headerLine.Count(c => c == ';') >= headerLine.Count(c => c == ',')
            ? ';'
            : ',';

        var headers = SplitCsvLine(headerLine, separator)
            .Select(NormalizeHeader)
            .ToArray();

        var rows = new List<Dictionary<string,string?>>();
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = SplitCsvLine(line, separator);
            var row = new Dictionary<string,string?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Length; i++)
            {
                row[headers[i]] = i < values.Count ? values[i].Trim() : null;
            }
            rows.Add(row);
        }
        return rows;
    }

    internal static List<Dictionary<string,string?>> ReadXlsx(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException("Planilha sem abas.");

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        if (lastRow < 1 || lastColumn < 1)
            throw new InvalidDataException("Planilha vazia.");

        var headers = Enumerable.Range(1, lastColumn)
            .Select(column => NormalizeHeader(sheet.Cell(1, column).GetString()))
            .ToArray();

        var rows = new List<Dictionary<string,string?>>();
        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var row = new Dictionary<string,string?>(StringComparer.OrdinalIgnoreCase);
            var hasValue = false;
            for (var column = 1; column <= lastColumn; column++)
            {
                var value = sheet.Cell(rowNumber, column).GetFormattedString().Trim();
                if (value.Length > 0) hasValue = true;
                row[headers[column - 1]] = value.Length == 0 ? null : value;
            }
            if (hasValue) rows.Add(row);
        }

        return rows;
    }

    private static List<string> SplitCsvLine(string line, char separator)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var quoted = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (c == separator && !quoted)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result;
    }

    internal static string NormalizeHeader(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return new string(normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray())
            .Normalize(NormalizationForm.FormC)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);
    }

    private static string? Get(
        Dictionary<string,string?> row,
        params string[] aliases)
    {
        foreach (var alias in aliases.Select(NormalizeHeader))
        {
            if (row.TryGetValue(alias, out var value))
                return value;
        }
        return null;
    }

    private static string SafeMessage(Exception exception)
        => exception is DbUpdateException
            ? "conflito de banco de dados."
            : exception.Message;

    private static bool TryCondominium(ClaimsPrincipal user, out Guid condominiumId)
        => Guid.TryParse(user.FindFirst("condominium_id")?.Value, out condominiumId);
}
