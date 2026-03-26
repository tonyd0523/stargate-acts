using StargateAPI.Business.Data;
using StargateAPI.Business.Dtos;
using StargateAPI.Business.Queries;

namespace StargateAPI.Tests.Models
{
    /// <summary>
    /// Exercises entity and DTO property getters/setters that are otherwise only
    /// populated by EF Core or Dapper at runtime. Ensures coverlet marks them as hit.
    /// </summary>
    public class EntityAndDtoPropertyTests
    {
        [Fact]
        public void PersonAstronaut_AllProperties_CanBeSetAndRead()
        {
            var dto = new PersonAstronaut
            {
                PersonId = 1,
                Name = "Test",
                PhotoUrl = "photos/test.jpg",
                CurrentRank = "Colonel",
                CurrentDutyTitle = "Pilot",
                CareerStartDate = DateTime.Today,
                CareerEndDate = DateTime.Today.AddYears(5)
            };

            Assert.Equal(1, dto.PersonId);
            Assert.Equal("Test", dto.Name);
            Assert.Equal("photos/test.jpg", dto.PhotoUrl);
            Assert.Equal("Colonel", dto.CurrentRank);
            Assert.Equal("Pilot", dto.CurrentDutyTitle);
            Assert.Equal(DateTime.Today, dto.CareerStartDate);
            Assert.Equal(DateTime.Today.AddYears(5), dto.CareerEndDate);
        }

        [Fact]
        public void AstronautDutyWithPerson_AllProperties_CanBeSetAndRead()
        {
            var dto = new AstronautDutyWithPerson
            {
                Id = 1,
                PersonId = 2,
                PersonName = "Neil Armstrong",
                Rank = "Colonel",
                DutyTitle = "Commander",
                DutyStartDate = DateTime.Today,
                DutyEndDate = DateTime.Today.AddDays(30)
            };

            Assert.Equal(1, dto.Id);
            Assert.Equal(2, dto.PersonId);
            Assert.Equal("Neil Armstrong", dto.PersonName);
            Assert.Equal("Colonel", dto.Rank);
            Assert.Equal("Commander", dto.DutyTitle);
            Assert.NotNull(dto.DutyEndDate);
        }

        [Fact]
        public void Person_NavigationProperties_DefaultCorrectly()
        {
            var person = new Person();

            Assert.Null(person.AstronautDetail);
            Assert.NotNull(person.AstronautDuties);
            Assert.Empty(person.AstronautDuties);
        }

        [Fact]
        public void Person_NavigationProperties_CanBeSet()
        {
            var person = new Person { Name = "Test" };
            var detail = new AstronautDetail { PersonId = 1 };
            person.AstronautDetail = detail;

            Assert.Same(detail, person.AstronautDetail);
        }

        [Fact]
        public void AstronautDuty_PersonNavigation_DefaultsToNull()
        {
            var duty = new AstronautDuty();
            Assert.Null(duty.Person);
        }

        [Fact]
        public void AstronautDetail_PersonNavigation_DefaultsToNull()
        {
            var detail = new AstronautDetail();
            Assert.Null(detail.Person);
        }
    }
}
