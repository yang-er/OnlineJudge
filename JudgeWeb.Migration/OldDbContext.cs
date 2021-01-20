using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace JudgeWeb.Migration
{
    public partial class OldDbContext : DbContext
    {
        public OldDbContext()
        {
        }

        public OldDbContext(DbContextOptions<OldDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Balloon> Balloon { get; set; }
        public virtual DbSet<Contesto> Contest { get; set; }
        public virtual DbSet<ContestProb> ContestProb { get; set; }
        public virtual DbSet<ContestUser> ContestUser { get; set; }
        public virtual DbSet<GroupUser> GroupUser { get; set; }
        public virtual DbSet<Groups> Groups { get; set; }
        public virtual DbSet<History> History { get; set; }
        public virtual DbSet<Jp> Jp { get; set; }
        public virtual DbSet<LogHistory> LogHistory { get; set; }
        public virtual DbSet<Newso> News { get; set; }
        public virtual DbSet<PostSubject> PostSubject { get; set; }
        public virtual DbSet<PostWall> PostWall { get; set; }
        public virtual DbSet<ProbManager> ProbManager { get; set; }
        public virtual DbSet<ProbStat> ProbStat { get; set; }
        public virtual DbSet<Problemo> Problem { get; set; }
        public virtual DbSet<RunSource> RunSource { get; set; }
        public virtual DbSet<Sender> Sender { get; set; }
        public virtual DbSet<Sign> Sign { get; set; }
        public virtual DbSet<SourceLarge> SourceLarge { get; set; }
        public virtual DbSet<SubmitHide> SubmitHide { get; set; }
        public virtual DbSet<SubmitHist> SubmitHist { get; set; }
        public virtual DbSet<SubmitNow> SubmitNow { get; set; }
        public virtual DbSet<SysOptions> SysOptions { get; set; }
        public virtual DbSet<TagsDesc> TagsDesc { get; set; }
        public virtual DbSet<TeamInput> TeamInput { get; set; }
        public virtual DbSet<TeamPwd> TeamPwd { get; set; }
        public virtual DbSet<UserOnline> UserOnline { get; set; }
        public virtual DbSet<UserOptions> UserOptions { get; set; }
        public virtual DbSet<UserProbStat> UserProbStat { get; set; }
        public virtual DbSet<UserStat> UserStat { get; set; }
        public virtual DbSet<Userj> Userj { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.6-servicing-10079");

            modelBuilder.Entity<Balloon>(entity =>
            {
                entity.HasKey(e => e.Bid);

                entity.ToTable("balloon", "newjudge");

                entity.Property(e => e.Bid)
                    .HasColumnName("bid")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Cid)
                    .HasColumnName("cid")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Color)
                    .IsRequired()
                    .HasColumnName("color")
                    .HasColumnType("char(8)");

                entity.Property(e => e.Pid)
                    .HasColumnName("pid")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasColumnType("tinyint(4)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Submitid)
                    .HasColumnName("submitid")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Uid)
                    .HasColumnName("uid")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");
            });

            modelBuilder.Entity<Contesto>(entity =>
            {
                entity.HasKey(e => e.Cid);

                entity.ToTable("contest", "newjudge");

                entity.HasIndex(e => e.StartTime)
                    .HasName("start_time");

                entity.Property(e => e.Cid)
                    .HasColumnName("cid")
                    .HasColumnType("int(10) unsigned");

                entity.Property(e => e.Bonus)
                    .HasColumnName("bonus")
                    .HasColumnType("smallint(6)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Descr)
                    .IsRequired()
                    .HasColumnName("descr")
                    .IsUnicode(false);

                entity.Property(e => e.EndTime)
                    .HasColumnName("end_time")
                    .HasDefaultValueSql("0000-00-00 00:00:00");

                entity.Property(e => e.Fee)
                    .HasColumnName("fee")
                    .HasColumnType("smallint(6)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.JudgeType)
                    .IsRequired()
                    .HasColumnName("judge_type")
                    .HasColumnType("enum('joj','pc^2')")
                    .HasDefaultValueSql("joj");

                entity.Property(e => e.LastBalloonSid)
                    .HasColumnName("last_balloon_sid")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.OpenType)
                    .IsRequired()
                    .HasColumnName("open_type")
                    .HasColumnType("enum('open','restricted','special')")
                    .HasDefaultValueSql("open");

                entity.Property(e => e.Prize)
                    .HasColumnName("prize")
                    .HasColumnType("smallint(6)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Pwd)
                    .IsRequired()
                    .HasColumnName("pwd")
                    .HasMaxLength(8)
                    .IsUnicode(false);

                entity.Property(e => e.RunStatus)
                    .HasColumnName("run_status")
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValueSql("-9");

                entity.Property(e => e.StartTime)
                    .HasColumnName("start_time")
                    .HasDefaultValueSql("0000-00-00 00:00:00");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasColumnName("title")
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.Visible)
                    .IsRequired()
                    .HasColumnName("visible")
                    .HasColumnType("enum('Y','N')")
                    .HasDefaultValueSql("Y");
            });

            modelBuilder.Entity<ContestProb>(entity =>
            {
                entity.ToTable("contest_prob", "newjudge");

                entity.HasIndex(e => new { e.Cid, e.Pid })
                    .HasName("cid")
                    .IsUnique();

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Cid)
                    .HasColumnName("cid")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Color)
                    .HasColumnName("color")
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.Pid)
                    .HasColumnName("pid")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Seq)
                    .IsRequired()
                    .HasColumnName("seq")
                    .HasColumnType("char(1)");
            });

            modelBuilder.Entity<ContestUser>(entity =>
            {
                entity.HasKey(e => new { e.Cid, e.Uid });

                entity.ToTable("contest_user", "newjudge");

                entity.Property(e => e.Cid)
                    .HasColumnName("cid")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Uid)
                    .HasColumnName("uid")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Department)
                    .HasColumnName("department")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Email)
                    .HasColumnName("email")
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.Grade)
                    .HasColumnName("grade")
                    .HasMaxLength(4)
                    .IsUnicode(false);

                entity.Property(e => e.HidePrivacy)
                    .IsRequired()
                    .HasColumnName("hide_privacy")
                    .HasColumnType("enum('Y','N')")
                    .HasDefaultValueSql("Y");

                entity.Property(e => e.Ip)
                    .IsRequired()
                    .HasColumnName("ip")
                    .HasMaxLength(64)
                    .IsUnicode(false);

                entity.Property(e => e.Pc2name)
                    .HasColumnName("pc2name")
                    .HasColumnType("smallint(4)");

                entity.Property(e => e.Pc2pwd)
                    .HasColumnName("pc2pwd")
                    .HasMaxLength(8)
                    .IsUnicode(false);

                entity.Property(e => e.PersonalName)
                    .HasColumnName("personal_name")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Rank)
                    .HasColumnName("rank")
                    .HasColumnType("smallint(6)");

                entity.Property(e => e.SchoolId)
                    .HasColumnName("school_id")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.Sex)
                    .HasColumnName("sex")
                    .HasColumnType("enum('Female','Male')");

                entity.Property(e => e.SolveCnt)
                    .HasColumnName("solve_cnt")
                    .HasColumnType("tinyint(4)");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasColumnName("status")
                    .HasColumnType("enum('pending','accepted','admin','judge','reject')")
                    .HasDefaultValueSql("pending");

                entity.Property(e => e.TeamName)
                    .IsRequired()
                    .HasColumnName("team_name")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Telphone)
                    .HasColumnName("telphone")
                    .HasMaxLength(15)
                    .IsUnicode(false);

                entity.Property(e => e.University)
                    .HasColumnName("university")
                    .HasMaxLength(40)
                    .IsUnicode(false);

                entity.Property(e => e.Usertype)
                    .HasColumnName("usertype")
                    .HasColumnType("tinyint(2)")
                    .HasDefaultValueSql("0");
            });

            modelBuilder.Entity<GroupUser>(entity =>
            {
                entity.HasKey(e => new { e.Gid, e.Uid });

                entity.ToTable("group_user", "newjudge");

                entity.Property(e => e.Gid)
                    .HasColumnName("gid")
                    .HasColumnType("int(8) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Uid)
                    .HasColumnName("uid")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.JoinDate)
                    .HasColumnName("join_date")
                    .HasDefaultValueSql("0000-00-00 00:00:00");

                entity.Property(e => e.Slevel)
                    .HasColumnName("slevel")
                    .HasColumnType("tinyint(2)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.TrueName)
                    .IsRequired()
                    .HasColumnName("true_name")
                    .HasMaxLength(10)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Groups>(entity =>
            {
                entity.HasKey(e => e.Gid);

                entity.ToTable("groups", "newjudge");

                entity.HasIndex(e => e.Name)
                    .HasName("name")
                    .IsUnique();

                entity.Property(e => e.Gid)
                    .HasColumnName("gid")
                    .HasColumnType("int(8) unsigned");

                entity.Property(e => e.Creator)
                    .HasColumnName("creator")
                    .HasColumnType("int(10)");

                entity.Property(e => e.DateCreate)
                    .HasColumnName("date_create")
                    .HasDefaultValueSql("0000-00-00 00:00:00");

                entity.Property(e => e.ImageFile)
                    .HasColumnName("image_file")
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.Open)
                    .IsRequired()
                    .HasColumnName("open")
                    .HasColumnType("enum('Y','N')")
                    .HasDefaultValueSql("Y");

                entity.Property(e => e.Plan)
                    .HasColumnName("plan")
                    .HasMaxLength(255)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<History>(entity =>
            {
                entity.HasKey(e => e.Hid);

                entity.ToTable("history", "newjudge");

                entity.HasIndex(e => e.Sdate)
                    .HasName("date");

                entity.HasIndex(e => e.Uid)
                    .HasName("uid");

                entity.Property(e => e.Hid)
                    .HasColumnName("hid")
                    .HasColumnType("int(10) unsigned");

                entity.Property(e => e.ChanPoint)
                    .HasColumnName("chan_point")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Info)
                    .HasColumnName("info")
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.Opponent)
                    .HasColumnName("opponent")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.OrigPoint)
                    .HasColumnName("orig_point")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Runid)
                    .HasColumnName("runid")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Sdate)
                    .HasColumnName("sdate")
                    .HasDefaultValueSql("0000-00-00 00:00:00");

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasColumnName("type")
                    .HasColumnType("enum('special','normal')")
                    .HasDefaultValueSql("normal");

                entity.Property(e => e.Uid)
                    .HasColumnName("uid")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValueSql("0");
            });

            modelBuilder.Entity<Jp>(entity =>
            {
                entity.HasKey(e => e.Uid);

                entity.ToTable("jp", "newjudge");

                entity.Property(e => e.Uid)
                    .HasColumnName("uid")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Psum)
                    .HasColumnName("psum")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Ptrade)
                    .HasColumnName("ptrade")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");
            });

            modelBuilder.Entity<LogHistory>(entity =>
            {
                entity.ToTable("log_history", "newjudge");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.IpAddr)
                    .IsRequired()
                    .HasColumnName("ip_addr")
                    .HasMaxLength(40)
                    .IsUnicode(false);

                entity.Property(e => e.LogTime)
                    .HasColumnName("log_time")
                    .HasDefaultValueSql("0000-00-00 00:00:00");

                entity.Property(e => e.Port)
                    .HasColumnName("port")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasColumnName("status")
                    .HasColumnType("enum('Y','N','S')")
                    .HasDefaultValueSql("Y");

                entity.Property(e => e.Userid)
                    .IsRequired()
                    .HasColumnName("userid")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("0");

                entity.Property(e => e.WrongPwd)
                    .IsRequired()
                    .HasColumnName("wrong_pwd")
                    .HasMaxLength(16)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Newso>(entity =>
            {
                entity.ToTable("news", "newjudge");

                entity.HasKey(e => e.NewsId);

                entity.Property(e => e.NewsId)
                    .HasColumnName("news_id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.BeginTime).HasColumnName("begin_time");

                entity.Property(e => e.EndTime).HasColumnName("end_time");

                entity.Property(e => e.Message)
                    .IsRequired()
                    .HasColumnName("message")
                    .IsUnicode(false);

                entity.Property(e => e.Seq)
                    .HasColumnName("seq")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");
            });

            modelBuilder.Entity<PostSubject>(entity =>
            {
                entity.HasKey(e => new { e.Category, e.Pid });

                entity.ToTable("post_subject", "newjudge");

                entity.Property(e => e.Category)
                    .HasColumnName("category")
                    .HasColumnType("enum('prob','spec','contest','news','group')")
                    .HasDefaultValueSql("prob");

                entity.Property(e => e.Pid)
                    .HasColumnName("pid")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.LastPostId)
                    .HasColumnName("last_post_id")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Message)
                    .HasColumnName("message")
                    .IsUnicode(false);

                entity.Property(e => e.PostDate)
                    .HasColumnName("post_date")
                    .HasDefaultValueSql("0000-00-00 00:00:00");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasColumnName("status")
                    .HasColumnType("enum('normal','hide','outdated')")
                    .HasDefaultValueSql("normal");

                entity.Property(e => e.SubNum)
                    .HasColumnName("sub_num")
                    .HasColumnType("int(8)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasColumnName("title")
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<PostWall>(entity =>
            {
                entity.HasKey(e => e.PostId);

                entity.ToTable("post_wall", "newjudge");

                entity.Property(e => e.PostId)
                    .HasColumnName("post_id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Category)
                    .IsRequired()
                    .HasColumnName("category")
                    .HasColumnType("enum('prob','spec','contest','news','group')")
                    .HasDefaultValueSql("prob");

                entity.Property(e => e.Message)
                    .IsRequired()
                    .HasColumnName("message")
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.Pid)
                    .HasColumnName("pid")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.PostDate)
                    .HasColumnName("post_date")
                    .HasDefaultValueSql("0000-00-00 00:00:00");

                entity.Property(e => e.Priority)
                    .HasColumnName("priority")
                    .HasColumnType("tinyint(2)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.ReplyId)
                    .HasColumnName("reply_id")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasColumnName("status")
                    .HasColumnType("enum('normal','hide','outdated')")
                    .HasDefaultValueSql("normal");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasColumnName("title")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Uid)
                    .HasColumnName("uid")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");
            });

            modelBuilder.Entity<ProbManager>(entity =>
            {
                entity.HasKey(e => e.Pid);

                entity.ToTable("prob_manager", "newjudge");

                entity.Property(e => e.Pid)
                    .HasColumnName("pid")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Outdate).HasColumnName("outdate");

                entity.Property(e => e.Uid)
                    .HasColumnName("uid")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");
            });

            modelBuilder.Entity<ProbStat>(entity =>
            {
                entity.HasKey(e => e.Pid);

                entity.ToTable("prob_stat", "newjudge");

                entity.Property(e => e.Pid)
                    .HasColumnName("pid")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.BestSid)
                    .HasColumnName("best_sid")
                    .HasColumnType("int(11) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.CAc)
                    .HasColumnName("c_ac")
                    .HasColumnType("int(6) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.CAll)
                    .HasColumnName("c_all")
                    .HasColumnType("int(6) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.CCe)
                    .HasColumnName("c_ce")
                    .HasColumnType("int(6) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.CMle)
                    .HasColumnName("c_mle")
                    .HasColumnType("int(6) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.COther)
                    .HasColumnName("c_other")
                    .HasColumnType("int(6) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.CPass)
                    .HasColumnName("c_pass")
                    .HasColumnType("int(6) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.CPe)
                    .HasColumnName("c_pe")
                    .HasColumnType("int(6) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.CRe)
                    .HasColumnName("c_re")
                    .HasColumnType("int(6) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.CTle)
                    .HasColumnName("c_tle")
                    .HasColumnType("int(6) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.CWa)
                    .HasColumnName("c_wa")
                    .HasColumnType("int(6) unsigned")
                    .HasDefaultValueSql("0");
            });

            modelBuilder.Entity<Problemo>(entity =>
            {
                entity.HasKey(e => e.Pid);

                entity.ToTable("problem", "newjudge");

                entity.HasIndex(e => e.Cid)
                    .HasName("cid");

                entity.HasIndex(e => e.Name)
                    .HasName("name");

                entity.HasIndex(e => e.Pid)
                    .HasName("pid")
                    .IsUnique();

                entity.HasIndex(e => e.Source)
                    .HasName("source");

                entity.Property(e => e.Pid)
                    .HasColumnName("pid")
                    .HasColumnType("int(10) unsigned");

                entity.Property(e => e.Challenge)
                    .HasColumnName("challenge")
                    .HasColumnType("tinyint(1) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Cid)
                    .HasColumnName("cid")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Date).HasColumnName("date");

                entity.Property(e => e.Diff)
                    .HasColumnName("diff")
                    .HasColumnType("smallint(6)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Intro)
                    .IsRequired()
                    .HasColumnName("intro")
                    .HasColumnType("longtext");

                entity.Property(e => e.Memorylimit)
                    .HasColumnName("memorylimit")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("32767");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.Point)
                    .HasColumnName("point")
                    .HasColumnType("int(4) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Source)
                    .HasColumnName("source")
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasDefaultValueSql("none");

                entity.Property(e => e.SpecJudge)
                    .IsRequired()
                    .HasColumnName("spec_judge")
                    .HasColumnType("enum('Y','N')")
                    .HasDefaultValueSql("N");

                entity.Property(e => e.Tags)
                    .IsRequired()
                    .HasColumnName("tags")
                    .HasMaxLength(40)
                    .IsUnicode(false);

                entity.Property(e => e.Timelimit)
                    .HasColumnName("timelimit")
                    .HasColumnType("smallint(6)")
                    .HasDefaultValueSql("30");

                entity.Property(e => e.Visible)
                    .IsRequired()
                    .HasColumnName("visible")
                    .HasColumnType("enum('show','hide','disable')")
                    .HasDefaultValueSql("show");
            });

            modelBuilder.Entity<RunSource>(entity =>
            {
                entity.HasKey(e => e.Sid);

                entity.ToTable("run_source", "newjudge");

                entity.Property(e => e.Sid)
                    .HasColumnName("sid")
                    .HasColumnType("int(10)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Info)
                    .HasColumnName("info")
                    .IsUnicode(false);

                entity.Property(e => e.Source)
                    .IsRequired()
                    .HasColumnName("source")
                    .HasColumnType("longtext");
            });

            modelBuilder.Entity<Sender>(entity =>
            {
                entity.HasKey(e => e.MsgId);

                entity.ToTable("sender", "newjudge");

                entity.HasIndex(e => e.ToUserid)
                    .HasName("to_userid");

                entity.HasIndex(e => new { e.FromUid, e.ToUid })
                    .HasName("from_uid");

                entity.HasIndex(e => new { e.ToUid, e.MsgType })
                    .HasName("to_uid");

                entity.Property(e => e.MsgId)
                    .HasColumnName("msg_id")
                    .HasColumnType("int(10) unsigned");

                entity.Property(e => e.DateMsg)
                    .HasColumnName("date_msg")
                    .HasDefaultValueSql("0000-00-00 00:00:00");

                entity.Property(e => e.DelF)
                    .IsRequired()
                    .HasColumnName("del_f")
                    .HasColumnType("enum('Y','N')")
                    .HasDefaultValueSql("N");

                entity.Property(e => e.DelT)
                    .IsRequired()
                    .HasColumnName("del_t")
                    .HasColumnType("enum('Y','N')")
                    .HasDefaultValueSql("N");

                entity.Property(e => e.FromUid)
                    .HasColumnName("from_uid")
                    .HasColumnType("int(10)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.FromUserid)
                    .IsRequired()
                    .HasColumnName("from_userid")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Message)
                    .HasColumnName("message")
                    .IsUnicode(false);

                entity.Property(e => e.MsgType)
                    .IsRequired()
                    .HasColumnName("msg_type")
                    .HasColumnType("enum('Person','Group','System')")
                    .HasDefaultValueSql("Person");

                entity.Property(e => e.ToUid)
                    .HasColumnName("to_uid")
                    .HasColumnType("int(10)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.ToUserid)
                    .IsRequired()
                    .HasColumnName("to_userid")
                    .HasMaxLength(20)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Sign>(entity =>
            {
                entity.HasKey(e => e.SchoolId);

                entity.ToTable("sign", "newjudge");

                entity.Property(e => e.SchoolId)
                    .HasColumnName("school_id")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .ValueGeneratedNever();

                entity.Property(e => e.Cardid)
                    .HasColumnName("cardid")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ChineseName)
                    .HasColumnName("chinese_name")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Department)
                    .HasColumnName("department")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Email)
                    .HasColumnName("email")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Fixphone)
                    .HasColumnName("fixphone")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Grade)
                    .HasColumnName("grade")
                    .HasMaxLength(5)
                    .IsUnicode(false);

                entity.Property(e => e.Ip)
                    .HasColumnName("ip")
                    .HasMaxLength(64)
                    .IsUnicode(false);

                entity.Property(e => e.Mobilephone)
                    .HasColumnName("mobilephone")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.NickName)
                    .HasColumnName("nick_name")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Qq)
                    .HasColumnName("qq")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Sex)
                    .HasColumnName("sex")
                    .HasMaxLength(10)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<SourceLarge>(entity =>
            {
                entity.HasKey(e => e.Sid);

                entity.ToTable("source_large", "newjudge");

                entity.Property(e => e.Sid)
                    .HasColumnName("sid")
                    .HasColumnType("int(10)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Info)
                    .HasColumnName("info")
                    .IsUnicode(false);

                entity.Property(e => e.Source)
                    .IsRequired()
                    .HasColumnName("source")
                    .HasColumnType("longtext");
            });

            modelBuilder.Entity<SubmitHide>(entity =>
            {
                entity.HasKey(e => e.Sid);

                entity.ToTable("submit_hide", "newjudge");

                entity.HasIndex(e => e.Cid)
                    .HasName("cid");

                entity.HasIndex(e => e.Pid)
                    .HasName("pid");

                entity.HasIndex(e => e.Sdate)
                    .HasName("sdate");

                entity.HasIndex(e => e.Userid)
                    .HasName("userid");

                entity.HasIndex(e => new { e.Status, e.Uid })
                    .HasName("status_uid");

                entity.HasIndex(e => new { e.Uid, e.Pid })
                    .HasName("idx_submit_uid_pid");

                entity.Property(e => e.Sid)
                    .HasColumnName("sid")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Cid)
                    .HasColumnName("cid")
                    .HasColumnType("smallint(5) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Isopen)
                    .IsRequired()
                    .HasColumnName("isopen")
                    .HasColumnType("enum('yes','no')")
                    .HasDefaultValueSql("yes");

                entity.Property(e => e.JudgeServer)
                    .HasColumnName("judge_server")
                    .HasColumnType("tinyint(4)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Lang)
                    .IsRequired()
                    .HasColumnName("lang")
                    .HasColumnType("enum('C++','PAS','Java','AnsiC')")
                    .HasDefaultValueSql("C++");

                entity.Property(e => e.PBest)
                    .HasColumnName("p_best")
                    .HasColumnType("tinyint(4)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.PCont)
                    .HasColumnName("p_cont")
                    .HasColumnType("tinyint(4)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.PSolve)
                    .HasColumnName("p_solve")
                    .HasColumnType("tinyint(4)")
                    .HasDefaultValueSql("-1");

                entity.Property(e => e.PTop)
                    .HasColumnName("p_top")
                    .HasColumnType("tinyint(4)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Pid)
                    .HasColumnName("pid")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Sdate)
                    .HasColumnName("sdate")
                    .HasDefaultValueSql("0000-00-00 00:00:00");

                entity.Property(e => e.Slen)
                    .HasColumnName("slen")
                    .HasColumnType("int(6)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Spendmem1)
                    .HasColumnName("spendmem1")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Spendmem2)
                    .HasColumnName("spendmem2")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Spendtime)
                    .HasColumnName("spendtime")
                    .HasDefaultValueSql("-1");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasColumnType("tinyint(4)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Uid)
                    .HasColumnName("uid")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Userid)
                    .IsRequired()
                    .HasColumnName("userid")
                    .HasMaxLength(20)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<SubmitHist>(entity =>
            {
                entity.HasKey(e => e.Sid);

                entity.ToTable("submit_hist", "newjudge");

                entity.HasIndex(e => e.Cid)
                    .HasName("cid");

                entity.HasIndex(e => e.Pid)
                    .HasName("pid");

                entity.HasIndex(e => e.Sdate)
                    .HasName("sdate");

                entity.HasIndex(e => e.Userid)
                    .HasName("userid");

                entity.HasIndex(e => new { e.Status, e.Uid })
                    .HasName("status_uid");

                entity.HasIndex(e => new { e.Uid, e.Pid })
                    .HasName("idx_submit_uid_pid");

                entity.Property(e => e.Sid)
                    .HasColumnName("sid")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Cid)
                    .HasColumnName("cid")
                    .HasColumnType("smallint(5) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Isopen)
                    .IsRequired()
                    .HasColumnName("isopen")
                    .HasColumnType("enum('yes','no')")
                    .HasDefaultValueSql("yes");

                entity.Property(e => e.JudgeServer)
                    .HasColumnName("judge_server")
                    .HasColumnType("tinyint(4)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Lang)
                    .IsRequired()
                    .HasColumnName("lang")
                    .HasColumnType("enum('C++','PAS','Java','AnsiC')")
                    .HasDefaultValueSql("C++");

                entity.Property(e => e.PBest)
                    .HasColumnName("p_best")
                    .HasColumnType("tinyint(4)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.PCont)
                    .HasColumnName("p_cont")
                    .HasColumnType("tinyint(4)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.PSolve)
                    .HasColumnName("p_solve")
                    .HasColumnType("tinyint(4)")
                    .HasDefaultValueSql("-1");

                entity.Property(e => e.PTop)
                    .HasColumnName("p_top")
                    .HasColumnType("tinyint(4)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Pid)
                    .HasColumnName("pid")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Sdate)
                    .HasColumnName("sdate")
                    .HasDefaultValueSql("0000-00-00 00:00:00");

                entity.Property(e => e.Slen)
                    .HasColumnName("slen")
                    .HasColumnType("int(6)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Spendmem1)
                    .HasColumnName("spendmem1")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Spendmem2)
                    .HasColumnName("spendmem2")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Spendtime)
                    .HasColumnName("spendtime")
                    .HasDefaultValueSql("-1");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasColumnType("tinyint(4)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Uid)
                    .HasColumnName("uid")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Userid)
                    .IsRequired()
                    .HasColumnName("userid")
                    .HasMaxLength(20)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<SubmitNow>(entity =>
            {
                entity.HasKey(e => e.Sid);

                entity.ToTable("submit_now", "newjudge");

                entity.HasIndex(e => new { e.Pid, e.Cid })
                    .HasName("pid_cid");

                entity.HasIndex(e => new { e.Priority, e.JudgeStat })
                    .HasName("pri_and_sid");

                entity.Property(e => e.Sid)
                    .HasColumnName("sid")
                    .HasColumnType("int(10)");

                entity.Property(e => e.Balloon)
                    .HasColumnName("balloon")
                    .HasColumnType("enum('not','balloon','repeat','send')");

                entity.Property(e => e.Cid)
                    .HasColumnName("cid")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Info)
                    .HasColumnName("info")
                    .IsUnicode(false);

                entity.Property(e => e.IpAddr)
                    .IsRequired()
                    .HasColumnName("ip_addr")
                    .HasMaxLength(40)
                    .IsUnicode(false);

                entity.Property(e => e.JudgeServer)
                    .HasColumnName("judge_server")
                    .HasColumnType("tinyint(4)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.JudgeStat)
                    .IsRequired()
                    .HasColumnName("judge_stat")
                    .HasColumnType("enum('Waiting','Compiling','Running','Judged','Delayed','Saving','Saved')")
                    .HasDefaultValueSql("Waiting");

                entity.Property(e => e.Lang)
                    .IsRequired()
                    .HasColumnName("lang")
                    .HasColumnType("enum('C++','PAS','Java','AnsiC')")
                    .HasDefaultValueSql("C++");

                entity.Property(e => e.OnlyCompile)
                    .IsRequired()
                    .HasColumnName("only_compile")
                    .HasColumnType("enum('Y','N')")
                    .HasDefaultValueSql("N");

                entity.Property(e => e.Pid)
                    .HasColumnName("pid")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Priority)
                    .HasColumnName("priority")
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValueSql("1");

                entity.Property(e => e.Sdate)
                    .HasColumnName("sdate")
                    .HasDefaultValueSql("0000-00-00 00:00:00");

                entity.Property(e => e.Source)
                    .HasColumnName("source")
                    .HasColumnType("longtext");

                entity.Property(e => e.Spendmem1)
                    .HasColumnName("spendmem1")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Spendmem2)
                    .HasColumnName("spendmem2")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Spendtime)
                    .HasColumnName("spendtime")
                    .HasDefaultValueSql("-1");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasColumnType("tinyint(2)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Uid)
                    .HasColumnName("uid")
                    .HasColumnType("int(10)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Userid)
                    .IsRequired()
                    .HasColumnName("userid")
                    .HasMaxLength(20)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<SysOptions>(entity =>
            {
                entity.HasKey(e => e.SysOptId);

                entity.ToTable("sys_options", "newjudge");

                entity.Property(e => e.SysOptId)
                    .HasColumnName("sys_opt_id")
                    .HasColumnType("int(4)");

                entity.Property(e => e.Category)
                    .HasColumnName("category")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.DataType)
                    .IsRequired()
                    .HasColumnName("data_type")
                    .HasColumnType("enum('S','N')")
                    .HasDefaultValueSql("S");

                entity.Property(e => e.NValue)
                    .HasColumnName("n_value")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.SValue)
                    .IsRequired()
                    .HasColumnName("s_value")
                    .HasMaxLength(200)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TagsDesc>(entity =>
            {
                entity.HasKey(e => e.TagId);

                entity.ToTable("tags_desc", "newjudge");

                entity.Property(e => e.TagId)
                    .HasColumnName("tag_id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Descr)
                    .HasColumnName("descr")
                    .HasMaxLength(40)
                    .IsUnicode(false);

                entity.Property(e => e.Keyword)
                    .IsRequired()
                    .HasColumnName("keyword")
                    .HasMaxLength(16)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TeamInput>(entity =>
            {
                entity.ToTable("team_input", "newjudge");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Cid)
                    .HasColumnName("cid")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasColumnName("email")
                    .HasMaxLength(40)
                    .IsUnicode(false);

                entity.Property(e => e.Pwd)
                    .IsRequired()
                    .HasColumnName("pwd")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Seq)
                    .IsRequired()
                    .HasColumnName("seq")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Tag)
                    .IsRequired()
                    .HasColumnName("tag")
                    .HasColumnType("enum('W','X','N')")
                    .HasDefaultValueSql("N");

                entity.Property(e => e.TeamName)
                    .IsRequired()
                    .HasColumnName("team_name")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Tel)
                    .IsRequired()
                    .HasColumnName("tel")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Uni)
                    .IsRequired()
                    .HasColumnName("uni")
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.Userid)
                    .IsRequired()
                    .HasColumnName("userid")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Usertype)
                    .HasColumnName("usertype")
                    .HasColumnType("tinyint(2)")
                    .HasDefaultValueSql("0");
            });

            modelBuilder.Entity<TeamPwd>(entity =>
            {
                entity.ToTable("team_pwd", "newjudge");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Pwd)
                    .IsRequired()
                    .HasColumnName("pwd")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Round)
                    .HasColumnName("round")
                    .HasColumnType("tinyint(2)")
                    .HasDefaultValueSql("1");

                entity.Property(e => e.Userid)
                    .IsRequired()
                    .HasColumnName("userid")
                    .HasMaxLength(20)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<UserOnline>(entity =>
            {
                entity.HasKey(e => e.Uid);

                entity.ToTable("user_online", "newjudge");

                entity.Property(e => e.Uid)
                    .HasColumnName("uid")
                    .HasColumnType("int(10)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Action)
                    .HasColumnName("action")
                    .HasMaxLength(16)
                    .IsUnicode(false);

                entity.Property(e => e.LastIp)
                    .HasColumnName("last_ip")
                    .HasMaxLength(40)
                    .IsUnicode(false);

                entity.Property(e => e.LastRank)
                    .HasColumnName("last_rank")
                    .HasColumnType("int(6)")
                    .HasDefaultValueSql("-1");

                entity.Property(e => e.LastTime)
                    .HasColumnName("last_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Para)
                    .HasColumnName("para")
                    .HasMaxLength(20)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<UserOptions>(entity =>
            {
                entity.HasKey(e => new { e.Uid, e.Varname });

                entity.ToTable("user_options", "newjudge");

                entity.Property(e => e.Uid)
                    .HasColumnName("uid")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Varname)
                    .HasColumnName("varname")
                    .HasMaxLength(16)
                    .IsUnicode(false);

                entity.Property(e => e.Svalue)
                    .IsRequired()
                    .HasColumnName("svalue")
                    .HasMaxLength(64)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<UserProbStat>(entity =>
            {
                entity.HasKey(e => new { e.Uid, e.Pid });

                entity.ToTable("user_prob_stat", "newjudge");

                entity.HasIndex(e => e.BestSid)
                    .HasName("best_sid");

                entity.HasIndex(e => e.ContSeq)
                    .HasName("cont_seq");

                entity.HasIndex(e => e.Pid)
                    .HasName("pid");

                entity.Property(e => e.Uid)
                    .HasColumnName("uid")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Pid)
                    .HasColumnName("pid")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.BestSid)
                    .HasColumnName("best_sid")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.ContSeq)
                    .HasColumnName("cont_seq")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.DateScore).HasColumnName("date_score");

                entity.Property(e => e.FirstSid)
                    .HasColumnName("first_sid")
                    .HasColumnType("int(10)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Score)
                    .HasColumnName("score")
                    .HasColumnType("tinyint(1)");

                entity.Property(e => e.SolveSid)
                    .HasColumnName("solve_sid")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");
            });

            modelBuilder.Entity<UserStat>(entity =>
            {
                entity.HasKey(e => e.Uid);

                entity.ToTable("user_stat", "newjudge");

                entity.Property(e => e.Uid)
                    .HasColumnName("uid")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.CAc)
                    .HasColumnName("c_ac")
                    .HasColumnType("int(6) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.CAll)
                    .HasColumnName("c_all")
                    .HasColumnType("int(6) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.CCe)
                    .HasColumnName("c_ce")
                    .HasColumnType("int(6) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.CMle)
                    .HasColumnName("c_mle")
                    .HasColumnType("int(6) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.COther)
                    .HasColumnName("c_other")
                    .HasColumnType("int(6) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.CPass)
                    .HasColumnName("c_pass")
                    .HasColumnType("int(6) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.CPe)
                    .HasColumnName("c_pe")
                    .HasColumnType("int(6) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.CRe)
                    .HasColumnName("c_re")
                    .HasColumnType("int(6) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.CTle)
                    .HasColumnName("c_tle")
                    .HasColumnType("int(6) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.CWa)
                    .HasColumnName("c_wa")
                    .HasColumnType("int(6) unsigned")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Jpoint)
                    .HasColumnName("jpoint")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("5");

                entity.Property(e => e.JpointAll)
                    .HasColumnName("jpoint_all")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.LastSubmitTime)
                    .HasColumnName("last_submit_time")
                    .HasDefaultValueSql("0000-00-00 00:00:00");
            });

            modelBuilder.Entity<Userj>(entity =>
            {
                entity.HasKey(e => e.Uid);

                entity.ToTable("userj", "newjudge");

                entity.HasIndex(e => e.Userid)
                    .HasName("userid");

                entity.Property(e => e.Uid)
                    .HasColumnName("uid")
                    .HasColumnType("int(10) unsigned");

                entity.Property(e => e.Dept)
                    .HasColumnName("dept")
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.Family)
                    .HasColumnName("family")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Mail)
                    .HasColumnName("mail")
                    .HasMaxLength(80)
                    .IsUnicode(false);

                entity.Property(e => e.Medal)
                    .HasColumnName("medal")
                    .HasColumnType("tinyint(2)")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasColumnName("password")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Plan)
                    .HasColumnName("plan")
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasDefaultValueSql("This guy is lazy, nothing left.");

                entity.Property(e => e.PwdSave)
                    .IsRequired()
                    .HasColumnName("pwd_save")
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.RegDate)
                    .HasColumnName("reg_date")
                    .HasColumnType("date")
                    .HasDefaultValueSql("0000-00-00");

                entity.Property(e => e.Sender)
                    .IsRequired()
                    .HasColumnName("sender")
                    .HasColumnType("enum('open','closed')")
                    .HasDefaultValueSql("open");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasColumnName("status")
                    .HasColumnType("enum('accepted','pending','temp','disable')")
                    .HasDefaultValueSql("accepted");

                entity.Property(e => e.University)
                    .HasColumnName("university")
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.Userid)
                    .IsRequired()
                    .HasColumnName("userid")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Usertype)
                    .IsRequired()
                    .HasColumnName("usertype")
                    .HasColumnType("enum('normal','test','coder','admin','spec')")
                    .HasDefaultValueSql("normal");
            });
        }
    }
}
