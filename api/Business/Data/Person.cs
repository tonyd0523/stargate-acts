using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace StargateAPI.Business.Data
{
    [Table("Person")]
    public class Person
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>Relative path to the person's photo (e.g. "photos/neil-armstrong.jpg"), or null if none.</summary>
        public string? PhotoUrl { get; set; }

        public virtual AstronautDetail? AstronautDetail { get; set; }

        public virtual ICollection<AstronautDuty> AstronautDuties { get; set; } = new HashSet<AstronautDuty>();

    }

    public class PersonConfiguration : IEntityTypeConfiguration<Person>
    {
        public void Configure(EntityTypeBuilder<Person> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
            // WHY: The README states "A Person is uniquely identified by their Name." Without
            // this index the database would allow two rows with the same name, which would
            // silently break any lookup-by-name logic. The index enforces the rule at the
            // storage layer so no application code can accidentally bypass it.
            builder.HasIndex(x => x.Name).IsUnique();
            builder.HasOne(z => z.AstronautDetail).WithOne(z => z.Person).HasForeignKey<AstronautDetail>(z => z.PersonId);
            builder.HasMany(z => z.AstronautDuties).WithOne(z => z.Person).HasForeignKey(z => z.PersonId);
        }
    }
}
