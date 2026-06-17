using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

public enum KategoriaProduktu
{
    DanieGlowne,
    Napoj,
    Deser
}

public enum StatusZamowienia
{
    Przyjete,
    WPrzygotowaniu,
    Gotowe,
    Odebrane
}

public class Produkt
{
    public string Nazwa { get; set; }
    public decimal Cena { get; set; }
    public KategoriaProduktu Kategoria { get; set; }

    public decimal ProcentPrzeceny { get; set; }

    public Produkt(string nazwa, decimal cena, KategoriaProduktu kategoria, decimal procentPrzeceny = 0)
    {
        Nazwa = nazwa;
        Cena = cena;
        Kategoria = kategoria;
        ProcentPrzeceny = procentPrzeceny;
    }
}

public class PozycjaZamowienia
{
    public Produkt Produkt { get; set; }
    public int Ilosc { get; set; }

    public PozycjaZamowienia(Produkt produkt, int ilosc)
    {
        Produkt = produkt;
        Ilosc = ilosc;
    }
    public decimal ObliczCenePoPrzecenie() {
        if (Produkt.ProcentPrzeceny > 0)
            return Produkt.Cena - (Produkt.Cena * Produkt.ProcentPrzeceny / 100);
        return Produkt.Cena;
    }
    public decimal ObliczWartosc()
    {
        return Produkt.Cena * Ilosc;
    }
}

public class RegulaCenowa
{
    public decimal ProcentZnizki { get; set; } = 15;
    public bool ZastosowanoZnizkeProduktowa {get; private set;} = false;

    public decimal ZastosujZnizki(List<PozycjaZamowienia> pozycje, decimal cena)
    {
        bool maDanie = pozycje.Any(p => p.Produkt.Kategoria == KategoriaProduktu.DanieGlowne);
        bool maNapoj = pozycje.Any(p => p.Produkt.Kategoria == KategoriaProduktu.Napoj);
        bool maDeser = pozycje.Any(p => p.Produkt.Kategoria == KategoriaProduktu.Deser);

        decimal znizkaZestawowa = 0;
        if (maDanie && maNapoj && maDeser)
            znizkaZestawowa = cena * ProcentZnizki / 100;
        else if (maDanie && maNapoj)
            znizkaZestawowa = 5;

        decimal maxPrzecena = pozycje.Max(p => p.Produkt.ProcentPrzeceny);
        decimal znizkaProduktowa = cena * maxPrzecena / 100;

        if (znizkaProduktowa > znizkaZestawowa)
        {
            ZastosowanoZnizkeProduktowa = true;
            cena -= znizkaProduktowa;
        }
        else
        {
            ZastosowanoZnizkeProduktowa = false;
            cena -= znizkaZestawowa;
        }

        return cena;
    }
}

public class KalkulatorCeny
{
    private RegulaCenowa regula;

    public KalkulatorCeny(RegulaCenowa regula)
    {
        this.regula = regula;
    }
    public bool CzyZastosowanoZnizkeProduktowa()
    {
        return regula.ZastosowanoZnizkeProduktowa;
    }

    public decimal ObliczNaleznosc(
        List<PozycjaZamowienia> pozycje)
    {
        decimal suma = pozycje.Sum(
            p => p.ObliczWartosc());
        suma = regula.ZastosujZnizki(pozycje, suma);

        return suma;
    }
}

public abstract class Zamowienie
{
    private List<PozycjaZamowienia> pozycje = new List<PozycjaZamowienia>();
    public StatusZamowienia Status { get; private set; }
    public DateTime DataZlozenia { get; private set; }

    public Zamowienie()
    {
        Status = StatusZamowienia.Przyjete;
        DataZlozenia = DateTime.Now;
    }

    public void DodajProdukt(Produkt produkt, int ilosc)
    {
        pozycje.Add(new PozycjaZamowienia(produkt, ilosc));
    }

    public List<PozycjaZamowienia> PobierzPozycje()
    {
        return pozycje;
    }

    public virtual decimal ObliczNaleznosc(KalkulatorCeny kalkulator)
    {
        return kalkulator.ObliczNaleznosc(pozycje);
    }
    public abstract string GenerujPodsumowanie(KalkulatorCeny kalkulator);
}
public class ZamowienieNaMiejscu : Zamowienie
{
    public int NumerStolika { get; set; }

    public ZamowienieNaMiejscu(int numerStolika) : base()
    {
        NumerStolika = numerStolika;
    }

    public override string GenerujPodsumowanie(KalkulatorCeny kalkulator)
    {
        decimal naleznosc = ObliczNaleznosc(kalkulator);
        string podsumowanie = $"=== ZAMÓWIENIE NA MIEJSCU (Data: {DataZlozenia}) ===\n";
        podsumowanie += $"Stolik numer: {NumerStolika}\n";
        podsumowanie += "Pozycje:\n";
        foreach (var poz in PobierzPozycje())
        {
            string przecenaInfo = (poz.Produkt.ProcentPrzeceny > 0 && kalkulator.CzyZastosowanoZnizkeProduktowa())
            ? $", przecena {poz.Produkt.ProcentPrzeceny}%"
            : "";
            podsumowanie += $"- {poz.Produkt.Nazwa} x{poz.Ilosc} ({poz.ObliczWartosc():F2} zł){przecenaInfo}\n";
        }
        podsumowanie += $"Do zapłaty: {naleznosc:F2} zł\n";
        podsumowanie += "--------------------------------------------------\n";
        return podsumowanie;
    }
}


public class ZamowienieWDowozie : Zamowienie
{
    public string AdresDostawy { get; set; }
    public decimal KosztDostawy { get; set; } = 12.50m;

    public ZamowienieWDowozie(string adresDostawy) : base()
    {
        AdresDostawy = adresDostawy;
    }

    public override decimal ObliczNaleznosc(KalkulatorCeny kalkulator)
    {
        return base.ObliczNaleznosc(kalkulator) + KosztDostawy;
    }

    public override string GenerujPodsumowanie(KalkulatorCeny kalkulator)
    {
        decimal naleznosc = ObliczNaleznosc(kalkulator);
        string podsumowanie = $"=== ZAMÓWIENIE Z DOWOZEM (Data: {DataZlozenia}) ===\n";
        podsumowanie += $"Adres dostawy: {AdresDostawy}\n";
        podsumowanie += "Pozycje:\n";
        foreach (var poz in PobierzPozycje())
        {
            string przecenaInfo = (poz.Produkt.ProcentPrzeceny > 0 && kalkulator.CzyZastosowanoZnizkeProduktowa())
            ? $", przecena {poz.Produkt.ProcentPrzeceny}%"
            : "";
            podsumowanie += $"- {poz.Produkt.Nazwa} x{poz.Ilosc} ({poz.ObliczWartosc()} zł){przecenaInfo}\n";
        }
        podsumowanie += $"Koszt dostawy: {KosztDostawy:F2} zł\n";
        podsumowanie += $"Do zapłaty (razem): {naleznosc:F2} zł\n";
        podsumowanie += "--------------------------------------------------\n";
        return podsumowanie;
    }
}
class Program
{
    static void Main()
    {
        string plikMenu = "menu.txt";
        string plikZamowien = "historia_zamowien.txt";


        if (!File.Exists(plikMenu))
        {

            File.WriteAllLines(plikMenu, new string[]
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
            foreach (string linia in File.ReadAllLines(plikMenu))
            {
                var dane = linia.Split(';');
                if (dane.Length == 4)
                {
                    string nazwa = dane[0];
                    decimal cena;
                    
                    if (!decimal.TryParse(dane[1], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.CurrentCulture, out cena)
                        && !decimal.TryParse(dane[1], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out cena))
                    {
                        
                        Console.WriteLine($"Nieprawidłowa cena w pliku: '{dane[1]}' dla produktu '{nazwa}'. Pozycja pominięta.");
                        continue;
                    }
                    KategoriaProduktu kat = (KategoriaProduktu)Enum.Parse(typeof(KategoriaProduktu), dane[2]);
                    

                    decimal przecena = 0;
                    if (dane.Length == 4)
                        decimal.TryParse(dane[3], System.Globalization.NumberStyles.Number,
                            System.Globalization.CultureInfo.CurrentCulture, out przecena);
                menu.Add(new Produkt(nazwa, cena, kat, przecena));
                }
            }

        KalkulatorCeny kalkulator = new KalkulatorCeny(new RegulaCenowa());

        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== MENU GŁÓWNE ===");
            Console.WriteLine("1. Zamówienie na miejscu");
            Console.WriteLine("2. Zamówienie na dowóz");
            Console.WriteLine("0. Wyjście");
            Console.Write("Wybierz opcję: ");
            string wybor = Console.ReadLine();

            if (wybor == "0") break;

            Zamowienie aktualne = null;

            if (wybor == "1")
            {
                Console.Write("Podaj numer stolika: ");
                int.TryParse(Console.ReadLine(), out int nr);
                aktualne = new ZamowienieNaMiejscu(nr);
            }
            else if (wybor == "2")
            {
                Console.Write("Podaj adres dostawy: ");
                string adres = Console.ReadLine();
                aktualne = new ZamowienieWDowozie(adres);
            }
            else continue;

            while (true)
            {
                Console.Clear();
                Console.WriteLine("--- DODAJ PRODUKTY (0 aby zakończyć) ---");
                Console.WriteLine("/ DANIA GŁÓWNE /");
                for (int i = 0; i < menu.Count; i++)
                {
                    if (menu[i].Kategoria == KategoriaProduktu.DanieGlowne)
                    {
                        string przecenaInfo = menu[i].ProcentPrzeceny > 0
                            ? $" ({menu[i].ProcentPrzeceny}% przeceny)"
                            : "";
                        Console.WriteLine($"{i + 1}. {menu[i].Nazwa} - {menu[i].Cena} zł{przecenaInfo}");
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
                        Console.WriteLine($"{i + 1}. {menu[i].Nazwa} - {menu[i].Cena} zł{przecenaInfo}");
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
                        Console.WriteLine($"{i + 1}. {menu[i].Nazwa} - {menu[i].Cena} zł{przecenaInfo}");
                    }
                }

                Console.Write("Wybór: ");
                string pWybor = Console.ReadLine();
                if (pWybor == "0") break;

                if (int.TryParse(pWybor, out int idx) && idx > 0 && idx <= menu.Count)
                {
                    Console.Write($"Ile sztuk {menu[idx - 1].Nazwa}?: ");
                    int.TryParse(Console.ReadLine(), out int ilosc);
                    if (ilosc > 0) aktualne.DodajProdukt(menu[idx - 1], ilosc);
                }
            }
            if (aktualne.PobierzPozycje().Count > 0)
            {
                string podsumowanie = aktualne.GenerujPodsumowanie(kalkulator);
                Console.Clear();
                Console.WriteLine(podsumowanie);

                File.AppendAllText(plikZamowien, podsumowanie + Environment.NewLine);
                Console.WriteLine("Zamówienie zapisano do pliku historia_zamowien.txt");
            }
            else
            {
                Console.WriteLine("Brak pozycji w zamówieniu. Zamówienie anulowane.");
                Console.WriteLine("Naciśnij Enter, aby wrócić do menu...");
                Console.ReadLine();
                continue;
            }

            Console.WriteLine("Naciśnij Enter...");
            Console.ReadLine();
        }
    }
}