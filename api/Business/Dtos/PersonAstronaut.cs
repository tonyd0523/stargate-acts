namespace StargateAPI.Business.Dtos
{
    // Read-only Dapper projection DTO. Populated by a SQL LEFT JOIN between Person and
    // AstronautDetail; not an EF Core tracked entity. Property names must exactly match
    // the SQL column aliases in the queries that populate it (e.g. "a.Id as PersonId").
    /// <summary>
    /// Combined view of a person and their current astronaut assignment (if any).
    /// </summary>
    public class PersonAstronaut
    {
        /// <summary>Unique person identifier.</summary>
        /// <example>1</example>
        public int PersonId { get; set; }

        /// <summary>Full name of the person.</summary>
        /// <example>Neil Armstrong</example>
        public string Name { get; set; } = string.Empty;

        /// <summary>Relative path to the person's photo, or null if none uploaded.</summary>
        /// <example>photos/neil-armstrong.jpg</example>
        public string? PhotoUrl { get; set; }

        // FIX: Changed CurrentRank and CurrentDutyTitle from non-nullable string to
        // nullable string?. The query uses LEFT JOIN AstronautDetail — a person with no
        // astronaut assignment returns NULL for these columns, and Dapper maps NULL directly
        // to null regardless of any C# initializer (= string.Empty) on the property.
        /// <summary>Current military/agency rank, or null if never assigned a duty.</summary>
        /// <example>Colonel</example>
        public string? CurrentRank { get; set; }

        /// <summary>Current duty title, or "RETIRED" if retired. Null if never assigned.</summary>
        /// <example>Mission Commander</example>
        public string? CurrentDutyTitle { get; set; }

        /// <summary>Date the astronaut's career began (first duty start date).</summary>
        /// <example>1962-03-01</example>
        public DateTime? CareerStartDate { get; set; }

        /// <summary>Date the astronaut's career ended. Null if still active.</summary>
        /// <example>1971-07-31</example>
        public DateTime? CareerEndDate { get; set; }
    }
}
