using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StargateAPI.Migrations
{
    /// <inheritdoc />
    public partial class SeedAstronautData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Person",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Neil Armstrong" },
                    { 2, "Buzz Aldrin" },
                    { 3, "Sally Ride" },
                    { 4, "Mae Jemison" },
                    { 5, "Chris Hadfield" },
                    { 6, "Valentina Tereshkova" }
                });

            migrationBuilder.InsertData(
                table: "AstronautDetail",
                columns: new[] { "Id", "CareerEndDate", "CareerStartDate", "CurrentDutyTitle", "CurrentRank", "PersonId" },
                values: new object[,]
                {
                    { 1, new DateTime(1971, 7, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1962, 3, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "RETIRED", "Colonel", 1 },
                    { 2, null, new DateTime(1963, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Mission Specialist", "Colonel", 2 },
                    { 3, null, new DateTime(1978, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Payload Commander", "Lieutenant", 3 },
                    { 4, null, new DateTime(1987, 6, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), "Science Mission Specialist", "Lieutenant", 4 },
                    { 5, null, new DateTime(1992, 12, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "ISS Commander", "Colonel", 5 },
                    { 6, null, new DateTime(1962, 2, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), "Senior Cosmonaut", "Major", 6 }
                });

            migrationBuilder.InsertData(
                table: "AstronautDuty",
                columns: new[] { "Id", "DutyEndDate", "DutyStartDate", "DutyTitle", "PersonId", "Rank" },
                values: new object[,]
                {
                    { 1, new DateTime(1965, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1962, 3, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Pilot", 1, "2LT" },
                    { 2, new DateTime(1969, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1965, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Flight Commander", 1, "1LT" },
                    { 3, new DateTime(1971, 7, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1969, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Mission Commander", 1, "Colonel" },
                    { 4, null, new DateTime(1971, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "RETIRED", 1, "Colonel" },
                    { 5, new DateTime(1966, 8, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1963, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Pilot", 2, "2LT" },
                    { 6, new DateTime(1969, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1966, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "LEM Pilot", 2, "Captain" },
                    { 7, null, new DateTime(1969, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Mission Specialist", 2, "Colonel" },
                    { 8, new DateTime(1983, 5, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1978, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Mission Specialist", 3, "Ensign" },
                    { 9, null, new DateTime(1983, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Payload Commander", 3, "Lieutenant" },
                    { 10, new DateTime(1992, 9, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1987, 6, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), "Mission Specialist", 4, "Ensign" },
                    { 11, null, new DateTime(1992, 9, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Science Mission Specialist", 4, "Lieutenant" },
                    { 12, new DateTime(1995, 10, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1992, 12, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Mission Specialist", 5, "2LT" },
                    { 13, new DateTime(2013, 5, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1995, 10, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), "Mission Specialist", 5, "Captain" },
                    { 14, null, new DateTime(2013, 5, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), "ISS Commander", 5, "Colonel" },
                    { 15, new DateTime(1963, 6, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1962, 2, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), "Cosmonaut", 6, "Junior Lieutenant" },
                    { 16, null, new DateTime(1963, 6, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), "Senior Cosmonaut", 6, "Major" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AstronautDetail",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "AstronautDetail",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "AstronautDetail",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "AstronautDetail",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "AstronautDetail",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "AstronautDetail",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "AstronautDuty",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "AstronautDuty",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "AstronautDuty",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "AstronautDuty",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "AstronautDuty",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "AstronautDuty",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "AstronautDuty",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "AstronautDuty",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "AstronautDuty",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "AstronautDuty",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "AstronautDuty",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "AstronautDuty",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "AstronautDuty",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "AstronautDuty",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "AstronautDuty",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "AstronautDuty",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Person",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Person",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Person",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Person",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Person",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Person",
                keyColumn: "Id",
                keyValue: 6);
        }
    }
}
