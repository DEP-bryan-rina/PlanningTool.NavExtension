using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using PlanningTool.NavExtension.Models;
using System.Data;

namespace PlanningTool.NavExtension.Pages.PhysicalInventory
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<PhysicalInventoryItem> Inventory { get; set; } = new();

        public List<string> Locations { get; set; } = new();
        public List<string> Zones { get; set; } = new();
        public List<string> InventoryGroups { get; set; } = new();
        public List<string> Bins { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int CountYear { get; set; } = DateTime.Now.Year;

        [BindProperty(SupportsGet = true)]
        public string LocationCode { get; set; } = "WMS";

        [BindProperty(SupportsGet = true)]
        public string ZoneCode { get; set; } = "ALU";

        [BindProperty(SupportsGet = true)]
        public string InventoryGroup { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string BinCode { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            await using SqlConnection connection =
                new SqlConnection(GetConnectionString());

            await connection.OpenAsync();

            await LoadLocationsAsync(connection);

            if (!Locations.Contains(LocationCode))
            {
                LocationCode =
                    Locations.FirstOrDefault()
                    ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(LocationCode))
            {
                return;
            }

            await LoadZonesAsync(connection);

            if (!Zones.Contains(ZoneCode))
            {
                ZoneCode =
                    Zones.FirstOrDefault()
                    ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(ZoneCode))
            {
                return;
            }

            await LoadBinsAsync(connection);

            if (!string.IsNullOrWhiteSpace(BinCode) &&
                !Bins.Contains(BinCode))
            {
                BinCode = string.Empty;
            }

            await LoadInventoryGroupsAsync(connection);

            if (!string.IsNullOrWhiteSpace(InventoryGroup) &&
                !InventoryGroups.Contains(InventoryGroup))
            {
                InventoryGroup = string.Empty;
            }

            await LoadInventoryAsync(connection);
        }

        private async Task LoadLocationsAsync(
            SqlConnection connection)
        {
            Locations.Clear();

            const string sql = @"
                SELECT DISTINCT
                    [Location Code]
                FROM [DEDON2019].[dbo].[DEDONPH$Warehouse Entry]
                WHERE ISNULL([Location Code], '') <> ''
                ORDER BY [Location Code];";

            await using SqlCommand command =
                new SqlCommand(sql, connection);

            await using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                string? value =
                    reader["Location Code"]?.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    Locations.Add(value.Trim());
                }
            }
        }

        private async Task LoadZonesAsync(
            SqlConnection connection)
        {
            Zones.Clear();

            const string sql = @"
                SELECT DISTINCT
                    [Zone Code]
                FROM [DEDON2019].[dbo].[DEDONPH$Warehouse Entry]
                WHERE [Location Code] = @LocationCode
                  AND ISNULL([Zone Code], '') <> ''
                ORDER BY [Zone Code];";

            await using SqlCommand command =
                new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@LocationCode",
                SqlDbType.NVarChar,
                20
            ).Value = LocationCode.Trim();

            await using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                string? value =
                    reader["Zone Code"]?.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    Zones.Add(value.Trim());
                }
            }
        }

        private async Task LoadBinsAsync(
            SqlConnection connection)
        {
            Bins.Clear();

            const string sql = @"
                SELECT DISTINCT
                    [Bin Code]
                FROM [DEDON2019].[dbo].[DEDONPH$Warehouse Entry]
                WHERE [Location Code] = @LocationCode
                  AND [Zone Code] = @ZoneCode
                  AND ISNULL([Bin Code], '') <> ''
                  AND [Bin Code] <> 'ADJUSTMENT'
                ORDER BY [Bin Code];";

            await using SqlCommand command =
                new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@LocationCode",
                SqlDbType.NVarChar,
                20
            ).Value = LocationCode.Trim();

            command.Parameters.Add(
                "@ZoneCode",
                SqlDbType.NVarChar,
                20
            ).Value = ZoneCode.Trim();

            await using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                string? value =
                    reader["Bin Code"]?.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    Bins.Add(value.Trim());
                }
            }
        }

        private async Task LoadInventoryGroupsAsync(
            SqlConnection connection)
        {
            InventoryGroups.Clear();

            const string sql = @"
                SELECT DISTINCT
                    IT.[Gen_ Prod_ Posting Group] AS InventoryGroup
                FROM [DEDON2019].[dbo].[DEDONPH$Warehouse Entry] WE
                INNER JOIN [DEDON2019].[dbo].[DEDONPH$Item] IT
                    ON IT.[No_] COLLATE DATABASE_DEFAULT
                     = WE.[Item No_] COLLATE DATABASE_DEFAULT
                WHERE WE.[Location Code] = @LocationCode
                  AND WE.[Zone Code] = @ZoneCode
                  AND
                  (
                      @BinCode IS NULL
                      OR WE.[Bin Code] = @BinCode
                  )
                  AND ISNULL(
                      IT.[Gen_ Prod_ Posting Group],
                      ''
                  ) <> ''
                  AND ISNULL(
                      WE.[Bin Code],
                      ''
                  ) <> 'ADJUSTMENT'
                ORDER BY InventoryGroup;";

            await using SqlCommand command =
                new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@LocationCode",
                SqlDbType.NVarChar,
                20
            ).Value = LocationCode.Trim();

            command.Parameters.Add(
                "@ZoneCode",
                SqlDbType.NVarChar,
                20
            ).Value = ZoneCode.Trim();

            command.Parameters.Add(
                "@BinCode",
                SqlDbType.NVarChar,
                20
            ).Value =
                string.IsNullOrWhiteSpace(BinCode)
                    ? DBNull.Value
                    : BinCode.Trim();

            await using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                string? value =
                    reader["InventoryGroup"]?.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    InventoryGroups.Add(value.Trim());
                }
            }
        }

        private async Task LoadInventoryAsync(
            SqlConnection connection)
        {
            Inventory.Clear();

            await using SqlCommand command =
                new SqlCommand(
                    "dbo._spGetPhysicalInventoryCount",
                    connection);

            command.CommandType =
                CommandType.StoredProcedure;

            command.Parameters.Add(
                "@CountYear",
                SqlDbType.Int
            ).Value = CountYear;

            command.Parameters.Add(
                "@LocationCode",
                SqlDbType.NVarChar,
                20
            ).Value = LocationCode.Trim();

            command.Parameters.Add(
                "@ZoneCode",
                SqlDbType.NVarChar,
                20
            ).Value = ZoneCode.Trim();

            command.Parameters.Add(
                "@InventoryGroup",
                SqlDbType.NVarChar,
                20
            ).Value =
                string.IsNullOrWhiteSpace(InventoryGroup)
                    ? DBNull.Value
                    : InventoryGroup.Trim();

            command.Parameters.Add(
                "@BinCode",
                SqlDbType.NVarChar,
                20
            ).Value =
                string.IsNullOrWhiteSpace(BinCode)
                    ? DBNull.Value
                    : BinCode.Trim();

            await using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                Inventory.Add(
                    new PhysicalInventoryItem
                    {
                        BinCode =
                            GetString(reader, "BinCode"),

                        ItemNo =
                            GetString(reader, "ItemNo")
                            ?? string.Empty,

                        Description =
                            GetString(reader, "Description"),

                        ExtendedText =
                            GetString(reader, "ExtendedText"),

                        ColorCode =
                            GetString(reader, "ColorCode"),

                        ColorForPC =
                            GetString(reader, "ColorForPC"),

                        LotNo =
                            GetString(reader, "LotNo"),

                        ExpirationDate =
                            GetDateTime(
                                reader,
                                "ExpirationDate"),

                        UoM =
                            GetString(reader, "UoM"),

                        QtyCalculated =
                            GetDecimal(
                                reader,
                                "QtyCalculated") ?? 0,

                        CountTagNo =
                            GetString(
                                reader,
                                "CountTagNo"),

                        Counter1 =
                            GetDecimal(reader, "Counter1"),

                        Counter2 =
                            GetDecimal(reader, "Counter2"),

                        Counter3 =
                            GetDecimal(reader, "Counter3"),

                        Counter4 =
                            GetDecimal(reader, "Counter4"),

                        Counter5 =
                            GetDecimal(reader, "Counter5"),

                        Counter6 =
                            GetDecimal(reader, "Counter6"),

                        Counter7 =
                            GetDecimal(reader, "Counter7"),

                        Counter8 =
                            GetDecimal(reader, "Counter8"),

                        Counter9 =
                            GetDecimal(reader, "Counter9"),

                        Counter10 =
                            GetDecimal(reader, "Counter10"),

                        Counter11 =
                            GetDecimal(reader, "Counter11"),

                        Counter12 =
                            GetDecimal(reader, "Counter12"),

                        Counter13 =
                            GetDecimal(reader, "Counter13"),

                        Counter14 =
                            GetDecimal(reader, "Counter14"),

                        Counter15 =
                            GetDecimal(reader, "Counter15"),

                        Counter16 =
                            GetDecimal(reader, "Counter16"),

                        Counter17 =
                            GetDecimal(reader, "Counter17"),

                        Counter18 =
                            GetDecimal(reader, "Counter18"),

                        Counter19 =
                            GetDecimal(reader, "Counter19"),

                        Counter20 =
                            GetDecimal(reader, "Counter20"),

                        CounterTotal =
                            GetDecimal(
                                reader,
                                "CounterTotal") ?? 0,

                        Variance =
                            GetDecimal(
                                reader,
                                "Variance") ?? 0
                    });
            }
        }

        public async Task<IActionResult> OnPostSaveCountAsync(
            string locationCode,
            string zoneCode,
            string? binCode,
            string itemNo,
            string? lotNo,
            string countTagNo,
            int counterNo,
            decimal quantity,
            int countYear)
        {
            locationCode =
                locationCode?.Trim()
                ?? string.Empty;

            zoneCode =
                zoneCode?.Trim()
                ?? string.Empty;

            binCode =
                string.IsNullOrWhiteSpace(binCode)
                    ? null
                    : binCode.Trim();

            itemNo =
                itemNo?.Trim()
                ?? string.Empty;

            lotNo =
                string.IsNullOrWhiteSpace(lotNo)
                    ? null
                    : lotNo.Trim();

            countTagNo =
                countTagNo?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(locationCode) ||
                string.IsNullOrWhiteSpace(zoneCode) ||
                string.IsNullOrWhiteSpace(itemNo))
            {
                return BadRequest(
                    "Location, Zone and Item No. are required.");
            }

            if (counterNo < 1 || counterNo > 20)
            {
                return BadRequest(
                    "Counter number must be between 1 and 20.");
            }

            if (quantity < 0)
            {
                return BadRequest(
                    "Counter quantity cannot be negative.");
            }

            if (string.IsNullOrWhiteSpace(countTagNo))
            {
                return BadRequest(
                    "Count Tag No. is required.");
            }

            if (countTagNo.Length > 20)
            {
                return BadRequest(
                    "Count Tag No. cannot exceed 20 digits.");
            }

            if (!countTagNo.All(char.IsDigit))
            {
                return BadRequest(
                    "Count Tag No. must contain numbers only.");
            }

            await using SqlConnection connection =
                new SqlConnection(GetConnectionString());

            await using SqlCommand command =
                new SqlCommand(
                    "dbo._spSavePhysicalInventoryCount",
                    connection);

            command.CommandType =
                CommandType.StoredProcedure;

            command.Parameters.Add(
                "@CountYear",
                SqlDbType.Int
            ).Value = countYear;

            command.Parameters.Add(
                "@LocationCode",
                SqlDbType.NVarChar,
                20
            ).Value = locationCode;

            command.Parameters.Add(
                "@ZoneCode",
                SqlDbType.NVarChar,
                20
            ).Value = zoneCode;

            command.Parameters.Add(
                "@BinCode",
                SqlDbType.NVarChar,
                20
            ).Value =
                string.IsNullOrWhiteSpace(binCode)
                    ? DBNull.Value
                    : binCode;

            command.Parameters.Add(
                "@ItemNo",
                SqlDbType.NVarChar,
                30
            ).Value = itemNo;

            command.Parameters.Add(
                "@LotNo",
                SqlDbType.NVarChar,
                50
            ).Value =
                string.IsNullOrWhiteSpace(lotNo)
                    ? DBNull.Value
                    : lotNo;

            command.Parameters.Add(
                "@CounterNo",
                SqlDbType.Int
            ).Value = counterNo;

            SqlParameter quantityParameter =
                command.Parameters.Add(
                    "@CounterQuantity",
                    SqlDbType.Decimal);

            quantityParameter.Precision = 18;
            quantityParameter.Scale = 4;
            quantityParameter.Value = quantity;

            command.Parameters.Add(
                "@CountTagNo",
                SqlDbType.VarChar,
                20
            ).Value = countTagNo;

            command.Parameters.Add(
                "@UserID",
                SqlDbType.NVarChar,
                100
            ).Value =
                User.Identity?.Name
                ?? "SYSTEM";

            await connection.OpenAsync();

            await command.ExecuteNonQueryAsync();

            return new JsonResult(
                new
                {
                    success = true,
                    countTagNo,
                    counterNo,
                    quantity
                });
        }

        private string GetConnectionString()
        {
            return
                _configuration.GetConnectionString(
                    "DevConnection")
                ?? throw new InvalidOperationException(
                    "DevConnection is not configured.");
        }

        private static string? GetString(
            SqlDataReader reader,
            string columnName)
        {
            int ordinal =
                reader.GetOrdinal(columnName);

            return reader.IsDBNull(ordinal)
                ? null
                : reader.GetString(ordinal);
        }

        private static decimal? GetDecimal(
            SqlDataReader reader,
            string columnName)
        {
            int ordinal =
                reader.GetOrdinal(columnName);

            return reader.IsDBNull(ordinal)
                ? null
                : Convert.ToDecimal(
                    reader.GetValue(ordinal));
        }

        private static DateTime? GetDateTime(
            SqlDataReader reader,
            string columnName)
        {
            int ordinal =
                reader.GetOrdinal(columnName);

            return reader.IsDBNull(ordinal)
                ? null
                : Convert.ToDateTime(
                    reader.GetValue(ordinal));
        }
    }
}