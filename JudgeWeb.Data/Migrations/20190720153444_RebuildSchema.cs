using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace JudgeWeb.Data.Migrations
{
    public partial class RebuildSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    UserName = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(maxLength: 256, nullable: true),
                    Email = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(nullable: false),
                    PasswordHash = table.Column<string>(nullable: true),
                    SecurityStamp = table.Column<string>(nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(nullable: false),
                    TwoFactorEnabled = table.Column<bool>(nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(nullable: true),
                    LockoutEnabled = table.Column<bool>(nullable: false),
                    AccessFailedCount = table.Column<int>(nullable: false),
                    NickName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    LogId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Time = table.Column<DateTime>(nullable: false),
                    UserName = table.Column<string>(nullable: false),
                    ContestId = table.Column<int>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    EntityId = table.Column<int>(nullable: false),
                    Comment = table.Column<string>(nullable: false),
                    Resolved = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.LogId);
                });

            migrationBuilder.CreateTable(
                name: "Configures",
                columns: table => new
                {
                    ConfigId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(unicode: false, maxLength: 128, nullable: false),
                    Value = table.Column<string>(unicode: false, nullable: false),
                    Type = table.Column<string>(nullable: true),
                    Public = table.Column<int>(nullable: false),
                    Category = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configures", x => x.ConfigId);
                });

            migrationBuilder.CreateTable(
                name: "Contests",
                columns: table => new
                {
                    ContestId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: false),
                    ShortName = table.Column<string>(nullable: false),
                    StartTime = table.Column<DateTime>(nullable: true),
                    FreezeTime = table.Column<DateTime>(nullable: true),
                    EndTime = table.Column<DateTime>(nullable: true),
                    UnfreezeTime = table.Column<DateTime>(nullable: true),
                    RankingStrategy = table.Column<int>(nullable: false),
                    IsPublic = table.Column<bool>(nullable: false),
                    RegisterDefaultCategory = table.Column<int>(nullable: false),
                    GoldMedal = table.Column<int>(nullable: false),
                    SilverMedal = table.Column<int>(nullable: false),
                    BronzeMedal = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contests", x => x.ContestId);
                });

            migrationBuilder.CreateTable(
                name: "Executable",
                columns: table => new
                {
                    ExecId = table.Column<string>(unicode: false, maxLength: 64, nullable: false),
                    Md5sum = table.Column<string>(unicode: false, maxLength: 32, nullable: false),
                    ZipFile = table.Column<byte[]>(nullable: false),
                    ZipSize = table.Column<int>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    Type = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Executable", x => x.ExecId);
                });

            migrationBuilder.CreateTable(
                name: "InternalErrors",
                columns: table => new
                {
                    ErrorId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ContestId = table.Column<int>(nullable: true),
                    JudgingId = table.Column<int>(nullable: true),
                    Description = table.Column<string>(nullable: false),
                    JudgehostLog = table.Column<string>(nullable: false),
                    Time = table.Column<DateTimeOffset>(nullable: false),
                    Disabled = table.Column<string>(nullable: false),
                    Status = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternalErrors", x => x.ErrorId);
                });

            migrationBuilder.CreateTable(
                name: "JudgeHosts",
                columns: table => new
                {
                    ServerId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ServerName = table.Column<string>(unicode: false, maxLength: 64, nullable: false),
                    Active = table.Column<bool>(nullable: false),
                    PollTime = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JudgeHosts", x => x.ServerId);
                });

            migrationBuilder.CreateTable(
                name: "News",
                columns: table => new
                {
                    NewsId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Active = table.Column<bool>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    LastUpdate = table.Column<DateTime>(nullable: false),
                    Source = table.Column<byte[]>(nullable: true),
                    Content = table.Column<byte[]>(nullable: true),
                    Tree = table.Column<byte[]>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_News", x => x.NewsId);
                });

            migrationBuilder.CreateTable(
                name: "RankCache",
                columns: table => new
                {
                    ContestId = table.Column<int>(nullable: false),
                    TeamId = table.Column<int>(nullable: false),
                    PointsRestricted = table.Column<int>(nullable: false),
                    TotalTimeRestricted = table.Column<int>(nullable: false),
                    PointsPublic = table.Column<int>(nullable: false),
                    TotalTimePublic = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RankCache", x => new { x.ContestId, x.TeamId });
                });

            migrationBuilder.CreateTable(
                name: "Rejudges",
                columns: table => new
                {
                    RejudgeId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ContestId = table.Column<int>(nullable: false),
                    Reason = table.Column<string>(nullable: false),
                    Applied = table.Column<bool>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rejudges", x => x.RejudgeId);
                });

            migrationBuilder.CreateTable(
                name: "ScoreCache",
                columns: table => new
                {
                    ContestId = table.Column<int>(nullable: false),
                    TeamId = table.Column<int>(nullable: false),
                    ProblemId = table.Column<int>(nullable: false),
                    SubmissionRestricted = table.Column<int>(nullable: false),
                    PendingRestricted = table.Column<int>(nullable: false),
                    SolveTimeRestricted = table.Column<double>(nullable: false),
                    IsCorrectRestricted = table.Column<bool>(nullable: false),
                    SubmissionPublic = table.Column<int>(nullable: false),
                    PendingPublic = table.Column<int>(nullable: false),
                    SolveTimePublic = table.Column<double>(nullable: false),
                    IsCorrectPublic = table.Column<bool>(nullable: false),
                    FirstToSolve = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreCache", x => new { x.ContestId, x.TeamId, x.ProblemId });
                });

            migrationBuilder.CreateTable(
                name: "TeamAffiliations",
                columns: table => new
                {
                    AffiliationId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    FormalName = table.Column<string>(nullable: true),
                    ExternalId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamAffiliations", x => x.AffiliationId);
                });

            migrationBuilder.CreateTable(
                name: "TeamCategories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: false),
                    Color = table.Column<string>(nullable: false),
                    SortOrder = table.Column<int>(nullable: false),
                    IsPublic = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamCategories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    RoleId = table.Column<int>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(nullable: false),
                    ProviderKey = table.Column<string>(nullable: false),
                    ProviderDisplayName = table.Column<string>(nullable: true),
                    UserId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(nullable: false),
                    RoleId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(nullable: false),
                    LoginProvider = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Clarifications",
                columns: table => new
                {
                    ClarificationId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ContestId = table.Column<int>(nullable: false),
                    ResponseToId = table.Column<int>(nullable: true),
                    SubmitTime = table.Column<DateTime>(nullable: false),
                    Sender = table.Column<int>(nullable: true),
                    Recipient = table.Column<int>(nullable: true),
                    JuryMember = table.Column<string>(nullable: true),
                    ProblemId = table.Column<int>(nullable: true),
                    Category = table.Column<int>(nullable: false),
                    Body = table.Column<string>(nullable: false),
                    Answered = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clarifications", x => x.ClarificationId);
                    table.ForeignKey(
                        name: "FK_Clarifications_Contests_ContestId",
                        column: x => x.ContestId,
                        principalTable: "Contests",
                        principalColumn: "ContestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    LangId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ExternalId = table.Column<string>(unicode: false, maxLength: 16, nullable: false),
                    Name = table.Column<string>(unicode: false, nullable: false),
                    FileExtension = table.Column<string>(nullable: true),
                    AllowSubmit = table.Column<bool>(nullable: false),
                    AllowJudge = table.Column<bool>(nullable: false),
                    TimeFactor = table.Column<double>(nullable: false),
                    CompileScript = table.Column<string>(unicode: false, maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.LangId);
                    table.ForeignKey(
                        name: "FK_Languages_Executable_CompileScript",
                        column: x => x.CompileScript,
                        principalTable: "Executable",
                        principalColumn: "ExecId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Problems",
                columns: table => new
                {
                    ProblemId = table.Column<int>(nullable: false),
                    Title = table.Column<string>(maxLength: 128, nullable: false),
                    Source = table.Column<string>(maxLength: 256, nullable: false),
                    Flag = table.Column<int>(nullable: false),
                    TimeLimit = table.Column<int>(nullable: false),
                    MemoryLimit = table.Column<int>(nullable: false),
                    RunScript = table.Column<string>(unicode: false, maxLength: 64, nullable: false),
                    CompareScript = table.Column<string>(unicode: false, maxLength: 64, nullable: false),
                    ComapreArguments = table.Column<string>(unicode: false, maxLength: 128, nullable: true),
                    CombinedRunCompare = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Problems", x => x.ProblemId);
                    table.ForeignKey(
                        name: "FK_Problems_Executable_CompareScript",
                        column: x => x.CompareScript,
                        principalTable: "Executable",
                        principalColumn: "ExecId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Problems_Executable_RunScript",
                        column: x => x.RunScript,
                        principalTable: "Executable",
                        principalColumn: "ExecId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    ContestId = table.Column<int>(nullable: false),
                    TeamId = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    TeamName = table.Column<string>(maxLength: 128, nullable: false),
                    AffiliationId = table.Column<int>(nullable: false),
                    CategoryId = table.Column<int>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    RegisterTime = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => new { x.ContestId, x.TeamId });
                    table.ForeignKey(
                        name: "FK_Teams_TeamAffiliations_AffiliationId",
                        column: x => x.AffiliationId,
                        principalTable: "TeamAffiliations",
                        principalColumn: "AffiliationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Teams_TeamCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "TeamCategories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Teams_Contests_ContestId",
                        column: x => x.ContestId,
                        principalTable: "Contests",
                        principalColumn: "ContestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContestProblem",
                columns: table => new
                {
                    ContestId = table.Column<int>(nullable: false),
                    ProblemId = table.Column<int>(nullable: false),
                    ShortName = table.Column<string>(unicode: false, maxLength: 10, nullable: false),
                    Rank = table.Column<int>(nullable: false),
                    AllowSubmit = table.Column<bool>(nullable: false),
                    AllowJudge = table.Column<bool>(nullable: false),
                    Color = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContestProblem", x => new { x.ContestId, x.ProblemId });
                    table.ForeignKey(
                        name: "FK_ContestProblem_Contests_ContestId",
                        column: x => x.ContestId,
                        principalTable: "Contests",
                        principalColumn: "ContestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContestProblem_Problems_ProblemId",
                        column: x => x.ProblemId,
                        principalTable: "Problems",
                        principalColumn: "ProblemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    SubmissionId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Time = table.Column<DateTimeOffset>(nullable: false),
                    ContestId = table.Column<int>(nullable: false),
                    Author = table.Column<int>(nullable: false),
                    ProblemId = table.Column<int>(nullable: false),
                    SourceCode = table.Column<string>(unicode: false, maxLength: 131072, nullable: false),
                    CodeLength = table.Column<int>(nullable: false),
                    Language = table.Column<int>(nullable: false),
                    Ip = table.Column<string>(unicode: false, maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.SubmissionId);
                    table.ForeignKey(
                        name: "FK_Submissions_Languages_Language",
                        column: x => x.Language,
                        principalTable: "Languages",
                        principalColumn: "LangId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Submissions_Problems_ProblemId",
                        column: x => x.ProblemId,
                        principalTable: "Problems",
                        principalColumn: "ProblemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Testcases",
                columns: table => new
                {
                    TestcaseId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProblemId = table.Column<int>(nullable: false),
                    IsSecret = table.Column<bool>(nullable: false),
                    Md5sumInput = table.Column<string>(unicode: false, maxLength: 32, nullable: false),
                    Md5sumOutput = table.Column<string>(unicode: false, maxLength: 32, nullable: false),
                    Input = table.Column<byte[]>(maxLength: 33554432, nullable: false),
                    InputLength = table.Column<int>(nullable: false),
                    Output = table.Column<byte[]>(maxLength: 4194304, nullable: false),
                    OutputLength = table.Column<int>(nullable: false),
                    Rank = table.Column<int>(nullable: false),
                    Point = table.Column<int>(nullable: false),
                    Description = table.Column<string>(maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Testcases", x => x.TestcaseId);
                    table.ForeignKey(
                        name: "FK_Testcases_Problems_ProblemId",
                        column: x => x.ProblemId,
                        principalTable: "Problems",
                        principalColumn: "ProblemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Judgings",
                columns: table => new
                {
                    JudgingId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Active = table.Column<bool>(nullable: false),
                    SubmissionId = table.Column<int>(nullable: false),
                    FullTest = table.Column<bool>(nullable: false),
                    StartTime = table.Column<DateTimeOffset>(nullable: true),
                    StopTime = table.Column<DateTimeOffset>(nullable: true),
                    ServerId = table.Column<int>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    ExecuteTime = table.Column<int>(nullable: false),
                    ExecuteMemory = table.Column<int>(nullable: false),
                    CompileError = table.Column<string>(unicode: false, maxLength: 131072, nullable: true),
                    RejudgeId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Judgings", x => x.JudgingId);
                    table.ForeignKey(
                        name: "FK_Judgings_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "SubmissionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Details",
                columns: table => new
                {
                    TestId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Status = table.Column<int>(nullable: false),
                    JudgingId = table.Column<int>(nullable: false),
                    TestcaseId = table.Column<int>(nullable: false),
                    ExecuteMemory = table.Column<int>(nullable: false),
                    ExecuteTime = table.Column<int>(nullable: false),
                    OutputSystem = table.Column<string>(unicode: false, maxLength: 131072, nullable: true),
                    OutputDiff = table.Column<string>(unicode: false, maxLength: 131072, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Details", x => x.TestId);
                    table.ForeignKey(
                        name: "FK_Details_Judgings_JudgingId",
                        column: x => x.JudgingId,
                        principalTable: "Judgings",
                        principalColumn: "JudgingId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Details_Testcases_TestcaseId",
                        column: x => x.TestcaseId,
                        principalTable: "Testcases",
                        principalColumn: "TestcaseId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ContestId",
                table: "AuditLogs",
                column: "ContestId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityId",
                table: "AuditLogs",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Resolved",
                table: "AuditLogs",
                column: "Resolved");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Type",
                table: "AuditLogs",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Clarifications_ContestId",
                table: "Clarifications",
                column: "ContestId");

            migrationBuilder.CreateIndex(
                name: "IX_Clarifications_Recipient",
                table: "Clarifications",
                column: "Recipient");

            migrationBuilder.CreateIndex(
                name: "IX_Clarifications_ResponseToId",
                table: "Clarifications",
                column: "ResponseToId");

            migrationBuilder.CreateIndex(
                name: "IX_Clarifications_Sender",
                table: "Clarifications",
                column: "Sender");

            migrationBuilder.CreateIndex(
                name: "IX_Configures_Name",
                table: "Configures",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ContestProblem_ProblemId",
                table: "ContestProblem",
                column: "ProblemId");

            migrationBuilder.CreateIndex(
                name: "IX_ContestProblem_Rank",
                table: "ContestProblem",
                column: "Rank");

            migrationBuilder.CreateIndex(
                name: "IX_Details_JudgingId",
                table: "Details",
                column: "JudgingId");

            migrationBuilder.CreateIndex(
                name: "IX_Details_TestcaseId",
                table: "Details",
                column: "TestcaseId");

            migrationBuilder.CreateIndex(
                name: "IX_InternalErrors_Status",
                table: "InternalErrors",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JudgeHosts_ServerName",
                table: "JudgeHosts",
                column: "ServerName");

            migrationBuilder.CreateIndex(
                name: "IX_Judgings_RejudgeId",
                table: "Judgings",
                column: "RejudgeId");

            migrationBuilder.CreateIndex(
                name: "IX_Judgings_ServerId",
                table: "Judgings",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Judgings_Status",
                table: "Judgings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Judgings_SubmissionId",
                table: "Judgings",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Languages_CompileScript",
                table: "Languages",
                column: "CompileScript");

            migrationBuilder.CreateIndex(
                name: "IX_Languages_ExternalId",
                table: "Languages",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_Problems_CompareScript",
                table: "Problems",
                column: "CompareScript");

            migrationBuilder.CreateIndex(
                name: "IX_Problems_RunScript",
                table: "Problems",
                column: "RunScript");

            migrationBuilder.CreateIndex(
                name: "IX_Rejudges_ContestId",
                table: "Rejudges",
                column: "ContestId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_Author",
                table: "Submissions",
                column: "Author");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ContestId",
                table: "Submissions",
                column: "ContestId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_Language",
                table: "Submissions",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ProblemId",
                table: "Submissions",
                column: "ProblemId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamAffiliations_ExternalId",
                table: "TeamAffiliations",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamCategories_IsPublic",
                table: "TeamCategories",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_TeamCategories_SortOrder",
                table: "TeamCategories",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_AffiliationId",
                table: "Teams",
                column: "AffiliationId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CategoryId",
                table: "Teams",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_Status",
                table: "Teams",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_UserId",
                table: "Teams",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Testcases_ProblemId",
                table: "Testcases",
                column: "ProblemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Clarifications");

            migrationBuilder.DropTable(
                name: "Configures");

            migrationBuilder.DropTable(
                name: "ContestProblem");

            migrationBuilder.DropTable(
                name: "Details");

            migrationBuilder.DropTable(
                name: "InternalErrors");

            migrationBuilder.DropTable(
                name: "JudgeHosts");

            migrationBuilder.DropTable(
                name: "News");

            migrationBuilder.DropTable(
                name: "RankCache");

            migrationBuilder.DropTable(
                name: "Rejudges");

            migrationBuilder.DropTable(
                name: "ScoreCache");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Judgings");

            migrationBuilder.DropTable(
                name: "Testcases");

            migrationBuilder.DropTable(
                name: "TeamAffiliations");

            migrationBuilder.DropTable(
                name: "TeamCategories");

            migrationBuilder.DropTable(
                name: "Contests");

            migrationBuilder.DropTable(
                name: "Submissions");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropTable(
                name: "Problems");

            migrationBuilder.DropTable(
                name: "Executable");
        }
    }
}
