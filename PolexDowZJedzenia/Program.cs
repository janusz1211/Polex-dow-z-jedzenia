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

    public Produkt(string nazwa, decimal cena, KategoriaProduktu kategoria)
    {
        Nazwa = nazwa;
        Cena = cena;
        Kategoria = kategoria;
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

    public decimal ObliczWartosc()
    {
        return Produkt.Cena * Ilosc;
    }
}

public class RegulaCenowa
{
    public decimal ProcentZnizki { get; set; } = 10;

    public decimal ZastosujPromocjeProcentowa(decimal cena)
    {
        return cena - (cena * ProcentZnizki / 100);
    }

    public decimal ZastosujZestawLunchowy(
        List<PozycjaZamowienia> pozycje,
        decimal cena)
    {
        bool maDanie = pozycje.Any(
            p => p.Produkt.Kategoria ==
            KategoriaProduktu.DanieGlowne);

        bool maNapoj = pozycje.Any(
            p => p.Produkt.Kategoria ==
            KategoriaProduktu.Napoj);

        if (maDanie && maNapoj)
        {
            cena -= 5;
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

    public decimal ObliczNaleznosc(
        List<PozycjaZamowienia> pozycje)
    {
        decimal suma = pozycje.Sum(
            p => p.ObliczWartosc());

        suma = regula.ZastosujPromocjeProcentowa(suma);

        suma = regula.ZastosujZestawLunchowy(
            pozycje,
            suma);

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
        string podsumowanie = $"=== ZAMÓWIENIE NA MIEJSCU (Data: {DataZlozenia}) ===\n";
        podsumowanie += $"Stolik numer: {NumerStolika}\n";
        podsumowanie += "Pozycje:\n";
        foreach (var poz in PobierzPozycje())
        {
            podsumowanie += $"- {poz.Produkt.Nazwa} x{poz.Ilosc} ({poz.ObliczWartosc()} zł)\n";
        }
        podsumowanie += $"Do zapłaty: {ObliczNaleznosc(kalkulator)} zł\n";
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
        string podsumowanie = $"=== ZAMÓWIENIE Z DOWOZEM (Data: {DataZlozenia}) ===\n";
        podsumowanie += $"Adres dostawy: {AdresDostawy}\n";
        podsumowanie += "Pozycje:\n";
        foreach (var poz in PobierzPozycje())
        {
            podsumowanie += $"- {poz.Produkt.Nazwa} x{poz.Ilosc} ({poz.ObliczWartosc()} zł)\n";
        }
        podsumowanie += $"Koszt dostawy: {KosztDostawy} zł\n";
        podsumowanie += $"Do zapłaty (razem): {ObliczNaleznosc(kalkulator)} zł\n";
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
                "Burger;25.50;DanieGlowne",
                "Cola;6.00;Napoj",
                "Sernik;12.00;Deser"
            });
        }


        List<Produkt> menu = new List<Produkt>();
            foreach (string linia in File.ReadAllLines(plikMenu))
            {
                var dane = linia.Split(';');
                if (dane.Length == 3)
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
                    menu.Add(new Produkt(nazwa, cena, kat));
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
                for (int i = 0; i < menu.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {menu[i].Nazwa} - {menu[i].Cena} zł");
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