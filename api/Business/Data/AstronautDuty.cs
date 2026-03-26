using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace StargateAPI.Business.Data
{
    // DutyEndDate = null is the sentinel for "this is the current assignment."
    // When a new duty is added, the previous duty's DutyEndDate is set to
    // (newDuty.DutyStartDate - 1 day) per the domain rules in the README.
    [Table("AstronautDuty")]
    public class AstronautDuty
    {
        public int Id { get; set; }

        public int PersonId { get; set; }

        public string Rank { get; set; } = string.Empty;

        public string DutyTitle { get; set; } = string.Empty;

        public DateTime DutyStartDate { get; set; }

        public DateTime? DutyEndDate { get; set; }

        // FIX: Made nullable. EF Core populates this navigation property only when explicitly
        // included via .Include() or lazy-loaded. Declaring it non-nullable caused CS8618 because
        // the constructor cannot guarantee it is set, and accessing it without loading would
        // silently return null at runtime despite the non-nullable declaration.
        public virtual Person? Person { get; set; }
    }

    public class AstronautDutyConfiguration : IEntityTypeConfiguration<AstronautDuty>
    {
        public void Configure(EntityTypeBuilder<AstronautDuty> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
        }
    }
}
