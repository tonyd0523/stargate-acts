using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace StargateAPI.Business.Data
{
    // AstronautDetail is a denormalized snapshot of a person's current astronaut status.
    // It caches CurrentRank, CurrentDutyTitle, CareerStartDate, and CareerEndDate from
    // the latest duty record so reads don't need to re-derive them from the full
    // AstronautDuty history. It is created and kept in sync by CreateAstronautDutyHandler
    // and UpdateAstronautDutyHandler on every career change.
    //
    // Tradeoff: if AstronautDuty rows are modified outside those handlers the snapshot
    // can become stale. This is safe as long as all writes flow through the MediatR pipeline.
    [Table("AstronautDetail")]
    public class AstronautDetail
    {
        public int Id { get; set; }

        public int PersonId { get; set; }

        public string CurrentRank { get; set; } = string.Empty;

        public string CurrentDutyTitle { get; set; } = string.Empty;

        // FIX: Changed to nullable to match the DB schema — the InitialCreate migration defines
        // this column as nullable: true. The original non-nullable declaration caused a mismatch
        // where EF Core could silently return a default DateTime(0001,01,01) instead of null
        // when no value was stored, producing incorrect data in API responses.
        public DateTime? CareerStartDate { get; set; }

        public DateTime? CareerEndDate { get; set; }

        // FIX: Made nullable — same reason as AstronautDuty.Person. Navigation properties are
        // not set by the constructor; they are populated by EF Core on demand. Non-nullable
        // was a false promise to the compiler and produced CS8618 warnings.
        public virtual Person? Person { get; set; }
    }

    public class AstronautDetailConfiguration : IEntityTypeConfiguration<AstronautDetail>
    {
        public void Configure(EntityTypeBuilder<AstronautDetail> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
        }
    }
}
