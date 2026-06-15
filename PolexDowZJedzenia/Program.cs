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

public class Zamowienie
{
    private List<PozycjaZamowienia> pozycje =
        new List<PozycjaZamowienia>();

    public StatusZamowienia Status { get; private set; }

    public Zamowienie()
    {
        Status = StatusZamowienia.Przyjete;
    }

    public void DodajProdukt(Produkt produkt, int ilosc)
    {
        pozycje.Add(
            new PozycjaZamowienia(produkt, ilosc));
    }

    public void UsunProdukt(PozycjaZamowienia pozycja)
    {
        pozycje.Remove(pozycja);
    }

    public void ZmienStatus(StatusZamowienia status)
    {
        Status = status;
    }

    public List<PozycjaZamowienia> PobierzPozycje()
    {
        return pozycje;
    }

    public decimal ObliczNaleznosc(
        KalkulatorCeny kalkulator)
    {
        return kalkulator.ObliczNaleznosc(pozycje);
    }
}

class Program
{
    static void Main()
    {
        Produkt burger =
            new Produkt(
                "Burger",
                25,
                KategoriaProduktu.DanieGlowne);

        Produkt cola =
            new Produkt(
                "Cola",
                8,
                KategoriaProduktu.Napoj);

        Zamowienie zamowienie =
            new Zamowienie();

        zamowienie.DodajProdukt(burger, 2);
        zamowienie.DodajProdukt(cola, 1);

        RegulaCenowa regula =
            new RegulaCenowa();

        KalkulatorCeny kalkulator =
            new KalkulatorCeny(regula);

        Console.WriteLine(
            $"Status: {zamowienie.Status}");

        Console.WriteLine(
            $"Do zapłaty: {zamowienie.ObliczNaleznosc(kalkulator)} zł");
    }
}