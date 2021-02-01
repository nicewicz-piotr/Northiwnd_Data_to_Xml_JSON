using System;
using CS7;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Storage;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Xml.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PracaZEFCore
{
    class Program
    {
        /*Metoda pozwalająca na serializację pobranych danych (Kategorii i przypisanych produktów) z bazy Northwind do postaci json*/
        public static void SerializujDoJson()
        {
            using (Northwind db = new Northwind())
            {
                var kategorie = db.Categories.Select(k => new { k.CategoryName, k.CategoryID }).ToArray();
                var produkty = db.Products.Select(p => new { p.ProductName, p.Discontinued, p.CategoryID }).ToArray();

                var zapytanie = kategorie.GroupJoin(produkty,
                                kategoria => kategoria.CategoryID,
                                produkt => produkt.CategoryID,
                                (Kategoria, Produkty) => new {Kategoria = Kategoria.CategoryName, Produkty = Produkty.Select(s => new {s.ProductName, s.Discontinued}).OrderBy(p => p.ProductName)}
                );


                var jss = new JsonSerializer();

                jss.Formatting = Formatting.Indented;
                jss.Serialize(Console.Out, zapytanie);
                

            }
        }

        /*Metoda pozwalająca na serializację pobranych danych (Kategorii i przypisanych produktów) z bazy Northwind do postaci XML*/
        public static string SerializujDoXml2()
        {
            XElement xmlElements = null;
            XDocument docXml = null;

            using (Northwind db = new Northwind())
            {
                var kategorie = db.Categories.Select(k => new { k.CategoryName, k.CategoryID }).ToArray();
                var produkty = db.Products.Select(p => new { p.ProductName, p.Discontinued, p.CategoryID }).ToArray();

                XNamespace aw = "http://www.przykladowy-link.com";

                xmlElements = new XElement(aw + "Categories", kategorie.GroupJoin(produkty,
                    kategoria => kategoria.CategoryID,
                    produkt => produkt.CategoryID,
                    (k, p) => new { k.CategoryName, p = p.OrderBy(x => x.ProductName) }).
                    Select(e => new XElement(aw + "Category", new XElement(aw + "CategoryName", e.CategoryName), new XElement(aw + "Products", e.p.Select(s => new XElement(aw + "Product", new XElement(aw + "ProductName", s.ProductName)))))));
            }

            docXml = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), xmlElements);

            var sb = new StringBuilder();

            using (var writer = new StringWriter(sb))
            {
                docXml.Save(writer);
            }

            return sb.ToString();
        }


        static void ZapytanieOKategorie()
        {
            using (Northwind db = new Northwind())
            {
                var fabrykaProtokolu = db.GetService<ILoggerFactory>();

                fabrykaProtokolu.AddProvider(new DostawcaProtokoluKonsoli());

                System.Console.WriteLine("Lista kategorii i liczba przypisanych im produktów:");
                //IQueryable<Category> kategorie = db.Categories.Include(c => c.Products);
                IQueryable<Category> kategorie;

                System.Console.WriteLine("Włączyć ładowanie chętne (T/N): ");
                bool ladowanieChetne = (Console.ReadKey().Key == ConsoleKey.T);
                bool ladowanieJawne = false;

                if (ladowanieChetne)
                {
                    kategorie = db.Categories.Include(c => c.Products);
                }
                else
                {
                    kategorie = db.Categories;
                    System.Console.WriteLine("Włączyć jawne ładowanie? (T/N): ");
                    ladowanieJawne = (Console.ReadKey().Key == ConsoleKey.T);
                    System.Console.WriteLine();
                }

                foreach (Category item in kategorie)
                {
                    if (ladowanieJawne)
                    {
                        System.Console.WriteLine("Jawnie załadować produkty z danej kategorii? (T/N): ");
                        if (Console.ReadKey().Key == ConsoleKey.T)
                        {
                            var produkty = db.Entry(item).Collection(c => c.Products);
                            if (!produkty.IsLoaded)
                                produkty.Load();
                        }
                        System.Console.WriteLine();

                    }


                    System.Console.WriteLine($"Kategoria {item.CategoryName} ma {item.Products.Count} produktów");
                }
            }
        }

        static void ZapytanieOProdukty()
        {

            using (Northwind db = new Northwind())
            {

                var fabrykaProtokolu = db.GetService<ILoggerFactory>();

                fabrykaProtokolu.AddProvider(new DostawcaProtokoluKonsoli());

                string cena = String.Empty;
                decimal result;

                do
                {
                    System.Console.WriteLine("Podaj cenę produktu");
                    cena = Console.ReadLine();
                }
                while (!decimal.TryParse(cena, out result));

                IQueryable<Product> products = db.Products.Where(p => p.Cost > result).OrderByDescending(p => p.Cost);

                foreach (Product item in products)
                {
                    System.Console.WriteLine($"Identyfikator produktu: {item.ProductID}, Nazwa produktu: {item.ProductName}, Koszt: {item.Cost:$#,##0.00} Ilość: {item.Stock}");
                }
            }
        }

        /*Dodaj produkt*/
        public static bool DodajProdukty(string nazwaProduktu, decimal? cena, int idKategorii)
        {
            using (Northwind db = new Northwind())
            {
                Product produkt = new Product
                {
                    CategoryID = idKategorii,
                    Cost = cena,
                    ProductName = nazwaProduktu
                };

                db.Products.Add(produkt);
                int zapisane = db.SaveChanges();

                return (zapisane == 1);

            }

        }

        /*Modyfikuj produkt*/
        public static bool ZwiekszCeneProduktu(string nazwaProduktu, decimal kwota)
        {
            using (Northwind db = new Northwind())
            {
                Product product = db.Products.First(p => p.ProductName == nazwaProduktu);
                product.Cost += kwota;
                int zapisane = db.SaveChanges();

                return (zapisane == 1);
            }
        }

        /*Usuń produkt*/
        public static bool UsunProdukty(string nazwaProduktu)
        {

            using (Northwind db = new Northwind())
            {
                using (IDbContextTransaction transaction = db.Database.BeginTransaction())
                {
                    System.Console.WriteLine($"Transakcja została uruchomiona z poziomu izolacji: {transaction.GetDbTransaction().IsolationLevel}");
                    System.Console.WriteLine($"Połączenie: {transaction.GetDbTransaction().Connection}");
                    IQueryable<Product> products = db.Products.Where(p => p.ProductName.StartsWith(nazwaProduktu));
                    db.RemoveRange(products);

                    int zapisane = db.SaveChanges();
                    transaction.Commit();
                    return (zapisane == 1);
                }
            }
        }

        /*Wypisz wszystkie produkty*/
        public static void WypiszProdukty()
        {
            using (Northwind db = new Northwind())
            {
                System.Console.WriteLine("|------------------------------------------------------------------------|");
                System.Console.WriteLine("| ID  |         Nazwa produktu              | Koszt    | Stan | Nieprod. |");
                foreach (Product item in db.Products.OrderByDescending(p => p.Cost))
                {
                    System.Console.WriteLine($"| {item.ProductID:000} | {item.ProductName,-35} | {item.Cost,8:$0.00} | {item.Stock,5} | {item.Discontinued} |");
                }
                System.Console.WriteLine("|------------------------------------------------------------------------|");
            }
        }

        static void Main(string[] args)
        {
            System.Console.WriteLine(SerializujDoXml2());

            SerializujDoJson();
            DodajProdukty("Świerze ryby", 500M, 6);
            WypiszProdukty();
            ZwiekszCeneProduktu("Świerze ryby", 20);
            WypiszProdukty();
            UsunProdukty("Świerze ryby");
            WypiszProdukty();

            ZapytanieOKategorie();
            System.Console.WriteLine("----------------");
            ZapytanieOProdukty();
        }
    }
}
