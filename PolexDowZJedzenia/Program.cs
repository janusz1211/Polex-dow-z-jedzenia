using System;
using System.Collections.Generic;
using System.Linq;

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

    public decimal PobierzCene()
    {
        return Cena;
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

public interface IRegulaCenowa
{
    decimal ObliczRabaty(List<PozycjaZamowienia> pozycje);
}

public class PromocjaProcentowa : IRegulaCenowa
{
    private decimal procentRabatu;

    public PromocjaProcentowa(decimal procentRabatu)
    {
        this.procentRabatu = procentRabatu;
    }

    public decimal ObliczRabaty(List<PozycjaZamowienia> pozycje)
    {
        decimal suma = pozycje.Sum(p => p.ObliczWartosc());
        return suma * (procentRabatu / 100m);
    }
}

public class ZestawLunchowy : IRegulaCenowa
{
    public decimal ObliczRabaty(List<PozycjaZamowienia> pozycje)
    {
        // Tutaj później można zaimplementować logikę zestawu
        return 0m;
    }
}

public class KalkulatorCeny
{
    private IRegulaCenowa regula;

    public KalkulatorCeny(IRegulaCenowa regula)
    {
        this.regula = regula;
    }

    public decimal ObliczNaleznosc(List<PozycjaZamowienia> pozycje)
    {
        decimal suma = pozycje.Sum(p => p.ObliczWartosc());
        decimal rabat = regula.ObliczRabaty(pozycje);

        return suma - rabat;
    }
}

public class Zamowienie
{
    private List<PozycjaZamowienia> pozycje =
        new List<PozycjaZamowienia>();

    public StatusZamowienia Status { get; private set; }

    public Zamowienie()
    {
        Status = StatusZamowienia.Przyjete;
    }

    public void DodajPozycje(PozycjaZamowienia pozycja)
    {
        pozycje.Add(pozycja);
    }

    public void UsunPozycje(PozycjaZamowienia pozycja)
    {
        pozycje.Remove(pozycja);
    }

    public void ZmienStatus(StatusZamowienia nowyStatus)
    {
        Status = nowyStatus;
    }

    public List<PozycjaZamowienia> PobierzPozycje()
    {
        return pozycje;
    }

    public decimal ObliczNaleznosc(KalkulatorCeny kalkulator)
    {
        return kalkulator.ObliczNaleznosc(pozycje);
    }
}

class Program
{
    static void Main()
    {
        Produkt burger = new Produkt(
            "Burger",
            25m,
            KategoriaProduktu.DanieGlowne);

        Produkt cola = new Produkt(
            "Cola",
            8m,
            KategoriaProduktu.Napoj);

        Zamowienie zamowienie = new Zamowienie();

        zamowienie.DodajPozycje(
            new PozycjaZamowienia(burger, 2));

        zamowienie.DodajPozycje(
            new PozycjaZamowienia(cola, 1));

        IRegulaCenowa promocja =
            new PromocjaProcentowa(10);

        KalkulatorCeny kalkulator =
            new KalkulatorCeny(promocja);

        decimal naleznosc =
            zamowienie.ObliczNaleznosc(kalkulator);

        Console.WriteLine("Stan zamówienia: " + zamowienie.Status);
        Console.WriteLine("Do zapłaty: " + naleznosc + " zł");

        Console.ReadKey();
    }
}
