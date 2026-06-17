using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    static void Main()
    {
        string plik = "menu.txt";

        if (!File.Exists(plik))
        {
            File.WriteAllLines(plik, new[]
            {
                "Burger;25.50;DanieGlowne;0",
                "Pizza Margherita;32.00;DanieGlowne;15",
                "Makaron Carbonara;29.00;DanieGlowne;0",
                "Łosoś z Grilla;45.00;DanieGlowne;10",
                "Kotlet Schabowy;28.50;DanieGlowne;0",
                "Pierogi z Mięsem;22.00;DanieGlowne;5",
                "Stek Wolowy;65.00;DanieGlowne;0",
                "Risotto Borowikowe;34.50;DanieGlowne;10",
                "Kurczak w Sosie Curry;27.00;DanieGlowne;0",
                "Placki Ziemniaczane;19.50;DanieGlowne;0",
                "Dorsz w Panierce;31.00;DanieGlowne;5",
                "Kebab XXL;40.00;DanieGlowne;5",

                "Cola;6.00;Napoj;10",
                "Lemoniada;9.00;Napoj;0",
                "Espresso;7.00;Napoj;0",
                "Kawa Mrożona;10.00;Napoj;0",
                "Sok Pomarańczowy;8.50;Napoj;0",
                "Sok Jablkowy;7.50;Napoj;0",
                "Herbata Czarna;8.00;Napoj;0",
                "Herbata Zielona;8.00;Napoj;0",
                "Woda Gazowana;5.00;Napoj;0",
                "Woda Niegazowana;5.00;Napoj;0",

                "Sernik;12.00;Deser;0",
                "Szarlotka;14.50;Deser;20",
                "Brownie;16.00;Deser;0",
                "Panna Cotta;13.50;Deser;0",
                "Tarta Cytrynowa;15.00;Deser;10",
                "Lody Waniliowe;11.00;Deser;0",
                "Beza;18.00;Deser;20",
                "Tiramisu;16.50;Deser;0"
            });
        }

        List<Produkt> menu = new List<Produkt>();

        foreach (var l in File.ReadAllLines(plik))
        {
            var d = l.Split(';');

            if (d.Length < 4)
                continue;

            string nazwa = d[0].Trim();
            string cenaTxt = d[1].Trim();
            string katTxt = d[2].Trim();
            string przecenaTxt = d[3].Trim();

            if (!decimal.TryParse(cenaTxt, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal cena))
                continue;

            if (!Enum.TryParse<KategoriaProduktu>(katTxt, true, out var kat))
                continue;

            decimal.TryParse(przecenaTxt, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal przecena);

            menu.Add(new Produkt(nazwa, cena, kat, przecena));
        }

        var kalk = new KalkulatorCeny(new RegulaCenowa());

        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== MENU GŁÓWNE ===");
            Console.WriteLine("1. Zamówienie na miejscu");
            Console.WriteLine("2. Zamówienie na dowóz");
            Console.WriteLine("0. Wyjście");
            Console.Write("Wybierz opcję: ");

            string w = Console.ReadLine();
            if (w == "0") break;

            Zamowienie z = null;

            if (w == "1")
            {
                int s = PobierzInt("Podaj numer stolika(1-100): ", 1, 100);
                z = new ZamowienieNaMiejscu(s);
            }
            else if (w == "2")
            {
                Console.Write("Podaj adres dostawy: ");
                z = new ZamowienieWDowozie(Console.ReadLine());
            }
            else
            {
                continue;
            }

            while (true)
            {
                Console.Clear();
                Console.WriteLine("--- DODAJ PRODUKTY ---");

                Console.WriteLine("/ DANIA GŁÓWNE /");
                for (int i = 0; i < menu.Count; i++)
                {
                    if (menu[i].Kategoria == KategoriaProduktu.DanieGlowne)
                    {
                        string przecenaInfo = menu[i].ProcentPrzeceny > 0
                            ? $" ({menu[i].ProcentPrzeceny}% przeceny)"
                            : "";
                        Console.WriteLine($"{i + 1}. {menu[i].Nazwa} - {menu[i].CenaPoPrzecenie()} zł{przecenaInfo}");
                    }
                }

                Console.WriteLine("/ NAPOJE /");
                for (int i = 0; i < menu.Count; i++)
                {
                    if (menu[i].Kategoria == KategoriaProduktu.Napoj)
                    {
                        string przecenaInfo = menu[i].ProcentPrzeceny > 0
                            ? $" ({menu[i].ProcentPrzeceny}% przeceny)"
                            : "";
                        Console.WriteLine($"{i + 1}. {menu[i].Nazwa} - {menu[i].CenaPoPrzecenie()} zł{przecenaInfo}");
                    }
                }

                Console.WriteLine("/ DESERY /");
                for (int i = 0; i < menu.Count; i++)
                {
                    if (menu[i].Kategoria == KategoriaProduktu.Deser)
                    {
                        string przecenaInfo = menu[i].ProcentPrzeceny > 0
                            ? $" ({menu[i].ProcentPrzeceny}% przeceny)"
                            : "";
                        Console.WriteLine($"{i + 1}. {menu[i].Nazwa} - {menu[i].CenaPoPrzecenie()} zł{przecenaInfo}");
                    }
                }

                Console.Write("Wybierz produkt (0 aby zakończyć): ");
                if (!int.TryParse(Console.ReadLine(), out int idx) || idx == 0)
                    break;

                if (idx < 1 || idx > menu.Count)
                {
                    Console.WriteLine("Niepoprawny numer produktu. Naciśnij klawisz...");
                    Console.ReadKey();
                    continue;
                }

                Console.Write("Ilość: ");
                int.TryParse(Console.ReadLine(), out int ilosc);

                if (ilosc > 0)
                    z.Dodaj(menu[idx - 1], ilosc);
            }

            if (z != null && z.Pobierz().Count > 0)
            {
                z.ZmienStatus(StatusZamowienia.WPrzygotowaniu);

                Console.Clear();
                Console.WriteLine("=== PODSUMOWANIE ZAMÓWIENIA ===");
                Console.WriteLine(z.Info(kalk));
                ZapiszDoHistorii(z.Info(kalk));
                if (kalk.CzyZestaw())
                {
                    Console.WriteLine("\n[PROMO] Przyznano 10% zniżki na zestaw (Danie + Napój + Deser)!");
                }

                Console.WriteLine("\nNaciśnij ENTER, aby wrócić do menu głównego...");
                Console.ReadLine();
            }
        }
    }
    static void ZapiszDoHistorii(string tresc)
    {
        string plik = "HistoriaZ.txt";

        File.AppendAllText(plik, tresc + Environment.NewLine + "------------------------" + Environment.NewLine);
    }
    static int PobierzInt(string msg, int min, int max)
    {
        while (true)
        {
            Console.Write(msg);

            if (int.TryParse(Console.ReadLine(), out int x) && x >= min && x <= max)
                return x;

            Console.WriteLine("Błędny zakres");
        }
    }
}