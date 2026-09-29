using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;

namespace ReklamHesap;

public record ProductRow(long Id, string Category, string Name, string? SubOption, string Unit, decimal Price);
public record ServiceRow(long Id, string Name, string Unit, decimal Price);

public static class Database
{
    public static string Folder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ReklamHesap");
    public static string DbPath => Path.Combine(Folder, "reklam_hesap.db");
    public static string ConnectionString => $"Data Source={DbPath}";

    public static void Initialize()
    {
        Directory.CreateDirectory(Folder);
        using var c = new SqliteConnection(ConnectionString);
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
        CREATE TABLE IF NOT EXISTS Products (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Category TEXT NOT NULL,
            Name TEXT NOT NULL,
            SubOption TEXT,
            Unit TEXT NOT NULL DEFAULT 'm2',
            Price REAL NOT NULL DEFAULT 0
        );
        CREATE TABLE IF NOT EXISTS ServicePrices (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Unit TEXT NOT NULL,
            Price REAL NOT NULL DEFAULT 0
        );
        CREATE TABLE IF NOT EXISTS CompositeMaterials (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Brand TEXT NOT NULL,
            ThicknessMm REAL NOT NULL,
            SheetWidthCm REAL NOT NULL,
            SheetLengthCm REAL NOT NULL,
            Price REAL NOT NULL DEFAULT 0
        );
        CREATE TABLE IF NOT EXISTS Jobs (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CustomerName TEXT,
            JobName TEXT,
            CreatedAt TEXT NOT NULL,
            TotalCost REAL NOT NULL DEFAULT 0,
            SalePrice REAL NOT NULL DEFAULT 0
        );
        """;
        cmd.ExecuteNonQuery();
        Seed();
    }

    static void Seed()
    {
        using var c = new SqliteConnection(ConnectionString);
        c.Open();

        var products = new (string category, string name, string? sub, string unit)[]
        {
            ("Folyo", "Laminasyon", "Var", "m2"),
            ("Folyo", "Laminasyon", "Yok", "m2"),
            ("Folyo", "Kumlama Folyo", null, "m2"),
            ("Folyo", "Kumlama Baskılı Folyo", null, "m2"),
            ("Folyo", "Gold Folyo", null, "m2"),
            ("Folyo", "Gümüş Folyo", null, "m2"),
            ("Folyo", "Şeffaf Folyo", null, "m2"),
            ("Folyo", "Simli Folyo", null, "m2"),
            ("Folyo", "Desenli Folyo", null, "m2"),
            ("Folyo", "Baskılı Folyo", null, "m2"),
            ("Folyo", "Kesim Folyosu", null, "m2"),
            ("Folyo", "Plotter Kesim", null, "m2"),
            ("Folyo", "Bas-Kes", null, "m2"),
            ("Cam", "Cam Kirli / Cam Temizliği", null, "m2"),
            ("Kompozit", "Kompozit 3mm", null, "m2"),
            ("Kompozit", "Kompozit 4.5mm", null, "m2"),
            ("Plastik", "Polikarbon", null, "m2"),
            ("Dekote", "Dekote 4.5mm", null, "m2"),
            ("Dekote", "Dekote 2.5mm", null, "m2"),
            ("Dekote", "Dekote 8mm", null, "m2"),
            ("Pleksi", "Şeffaf P. 6mm", null, "m2"),
            ("LED", "Şerit Led", null, "m2"),
            ("Profil", "Köşebent 20x20", null, "m2"),
            ("Profil", "Köşebent 30x30", null, "m2")
        };

        foreach (var x in products)
        {
            using var q = c.CreateCommand();
            q.CommandText = "SELECT COUNT(*) FROM Products WHERE Name=$n AND IFNULL(SubOption,'')=IFNULL($s,'')";
            q.Parameters.AddWithValue("$n", x.name);
            q.Parameters.AddWithValue("$s", (object?)x.sub ?? DBNull.Value);
            if (Convert.ToInt32(q.ExecuteScalar()) == 0)
            {
                using var i = c.CreateCommand();
                i.CommandText = "INSERT INTO Products(Category,Name,SubOption,Unit,Price) VALUES($c,$n,$s,$u,0)";
                i.Parameters.AddWithValue("$c", x.category);
                i.Parameters.AddWithValue("$n", x.name);
                i.Parameters.AddWithValue("$s", (object?)x.sub ?? DBNull.Value);
                i.Parameters.AddWithValue("$u", x.unit);
                i.ExecuteNonQuery();
            }
        }

        var services = new (string name, string unit)[]
        {
            ("Servis", "km"),
            ("Yemek", "kişi"),
            ("İskele", "iş"),
            ("Merdiven", "iş"),
            ("Vinç", "iş")
        };

        foreach (var x in services)
        {
            using var q = c.CreateCommand();
            q.CommandText = "SELECT COUNT(*) FROM ServicePrices WHERE Name=$n";
            q.Parameters.AddWithValue("$n", x.name);
            if (Convert.ToInt32(q.ExecuteScalar()) == 0)
            {
                using var i = c.CreateCommand();
                i.CommandText = "INSERT INTO ServicePrices(Name,Unit,Price) VALUES($n,$u,0)";
                i.Parameters.AddWithValue("$n", x.name);
                i.Parameters.AddWithValue("$u", x.unit);
                i.ExecuteNonQuery();
            }
        }
    }

    public static List<ProductRow> Products()
    {
        var list=new List<ProductRow>();
        using var c=new SqliteConnection(ConnectionString); c.Open();
        using var q=c.CreateCommand();
        q.CommandText="SELECT Id,Category,Name,SubOption,Unit,Price FROM Products ORDER BY Id";
        using var r=q.ExecuteReader();
        while(r.Read()) list.Add(new ProductRow(r.GetInt64(0),r.GetString(1),r.GetString(2),
            r.IsDBNull(3)?null:r.GetString(3),r.GetString(4),r.GetDecimal(5)));
        return list;
    }

    public static List<ServiceRow> Services()
    {
        var list=new List<ServiceRow>();
        using var c=new SqliteConnection(ConnectionString); c.Open();
        using var q=c.CreateCommand();
        q.CommandText="SELECT Id,Name,Unit,Price FROM ServicePrices ORDER BY Id";
        using var r=q.ExecuteReader();
        while(r.Read()) list.Add(new ServiceRow(r.GetInt64(0),r.GetString(1),r.GetString(2),r.GetDecimal(3)));
        return list;
    }

    public static void SetProductPrice(long id,decimal price)
    {
        using var c=new SqliteConnection(ConnectionString); c.Open();
        using var q=c.CreateCommand();
        q.CommandText="UPDATE Products SET Price=$p WHERE Id=$id";
        q.Parameters.AddWithValue("$p",price); q.Parameters.AddWithValue("$id",id);
        q.ExecuteNonQuery();
    }

    public static void SetServicePrice(long id,decimal price)
    {
        using var c=new SqliteConnection(ConnectionString); c.Open();
        using var q=c.CreateCommand();
        q.CommandText="UPDATE ServicePrices SET Price=$p WHERE Id=$id";
        q.Parameters.AddWithValue("$p",price); q.Parameters.AddWithValue("$id",id);
        q.ExecuteNonQuery();
    }
}
