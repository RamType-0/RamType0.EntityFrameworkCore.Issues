using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;

namespace RamType0.EntityFrameworkCore.Issues.Issue21221
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Issue21221()
        {
            
            using var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();
            var option = new DbContextOptionsBuilder<Db>().UseSqlite(connection) .Options;
            using(var db = new Db(option))
            {
                db.Database.EnsureCreated();
            }
            var a = new A();
            var b = new B();
            var a2b = new A2B(a, b);
            a.A2Bs.Add(a2b);
            using (var db = new Db(option))
            {
                
                db.As.Add(a);
                
                db.SaveChanges();
            }

            Assert.IsFalse(a.A2Bs.Add(a2b));
            
            using(var db = new Db(option))
            {
                var efCoreGeneratedA = db.As.Include(a=>a.A2Bs).Single();
                Assert.AreNotSame(a, efCoreGeneratedA);

                var efCoreGeneratedA2B = efCoreGeneratedA.A2Bs.Single();
                Assert.AreNotSame(a2b, efCoreGeneratedA2B);

                var efCoreGeneratedB = db.Bs.Single();
                Assert.AreNotSame(b, efCoreGeneratedB);

                

                Assert.IsFalse(efCoreGeneratedA.A2Bs.Add(efCoreGeneratedA2B));//Fails at here because of issue 21221
            }
        }
    }

    public class Db : DbContext
    {
        public Db(DbContextOptions options) : base(options)
        {
        }

        public DbSet<A> As { get; private set; } = default!;
        public DbSet<B> Bs { get; private set; } = default!;
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseSqlite("Filename=:memory:");
            base.OnConfiguring(optionsBuilder);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<A2B>().HasKey(x => new { x.AId, x.BId });
            modelBuilder.Entity<A2B>().HasOne(x => x.A).WithMany(a => a.A2Bs).HasForeignKey(x => x.AId);
            base.OnModelCreating(modelBuilder);
        }
    }

    public class A
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; private set; }


        public HashSet<A2B> A2Bs { get; } = new HashSet<A2B>();

    }

    public class B
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; private set; }
    }

    public class A2B : IEquatable<A2B?>
    {
        public A2B(A a, B b)
        {
            A = a;
            B = b;
        }
        [Obsolete("For EFCore entity generation")]
#pragma warning disable CS8618 // Null 非許容フィールドは初期化されていません。null 許容として宣言することを検討してください。
        private A2B() {  }
#pragma warning restore CS8618 // Null 非許容フィールドは初期化されていません。null 許容として宣言することを検討してください。

        public Guid AId { get; private set; }
        public A A { get; private set; }
        public Guid BId { get; private set; }
        public B B { get; private set; }

        public override bool Equals(object? obj)
        {
            return Equals(obj as A2B);
        }

        public bool Equals(A2B? other)
        {
            return other != null &&
                   EqualityComparer<A>.Default.Equals(A, other.A) &&
                   EqualityComparer<B>.Default.Equals(B, other.B);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(A, B);
        }

        public static bool operator ==(A2B? left, A2B? right)
        {
            return EqualityComparer<A2B>.Default.Equals(left, right);
        }

        public static bool operator !=(A2B? left, A2B? right)
        {
            return !(left == right);
        }
    }
}