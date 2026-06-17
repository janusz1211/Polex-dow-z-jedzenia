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
    public decimal ProcentPrzeceny { get; set; }

    public Produkt(string nazwa, decimal cena, KategoriaProduktu kat, decimal przecena)
    {
        Nazwa = nazwa;
        Cena = cena;
        Kategoria = kat;
        ProcentPrzeceny = przecena;
    }

    public decimal CenaPoPrzecenie()
    {
        return Cena - Oszczednosc();
    }

    public decimal Oszczednosc()
    {
        return Cena * ProcentPrzeceny / 100m;
    }
}

public class PozycjaZamowienia
{
    public Produkt Produkt { get; set; }
    public int Ilosc { get; set; }

    public PozycjaZamowienia(Produkt p, int i)
    {
        Produkt = p;
        Ilosc = i;
    }

    public decimal WartoscBezRabatow()
    {
        return Produkt.Cena * Ilosc;
    }

    public decimal WartoscPoProdukcie()
    {
        return Produkt.CenaPoPrzecenie() * Ilosc;
    }
}

public class RegulaCenowa
{
    public bool UzytoZestawu { get; private set; }

    public decimal Oblicz(List<PozycjaZamowienia> p, decimal suma)
    {
        UzytoZestawu = false;

        bool danie = p.Any(x => x.Produkt.Kategoria == KategoriaProduktu.DanieGlowne);
        bool napoj = p.Any(x => x.Produkt.Kategoria == KategoriaProduktu.Napoj);
        bool deser = p.Any(x => x.Produkt.Kategoria == KategoriaProduktu.Deser);

        decimal znizkaZestaw = 0;

        if (danie && napoj && deser)
        {
            znizkaZestaw = suma * 0.10m;
            UzytoZestawu = true;
        }

        decimal znizkaProduktowa = p.Sum(x => x.Produkt.Oszczednosc() * x.Ilosc);

        return suma - Math.Max(znizkaZestaw, znizkaProduktowa);
    }
}

public class KalkulatorCeny
{
    private RegulaCenowa r;

    public KalkulatorCeny(RegulaCenowa r)
    {
        this.r = r;
    }

    public decimal Oblicz(List<PozycjaZamowienia> p)
    {
        decimal suma = p.Sum(x => x.WartoscBezRabatow());
        return r.Oblicz(p, suma);
    }

    public bool CzyZestaw() => r.UzytoZestawu;
}

public abstract class Zamowienie
{
    private List<PozycjaZamowienia> pozycje = new List<PozycjaZamowienia>();

    public StatusZamowienia Status { get; private set; }
    public DateTime Data { get; private set; }

    public Zamowienie()
    {
        Status = StatusZamowienia.Przyjete;
        Data = DateTime.Now;
    }

    public void Dodaj(Produkt p, int i)
    {
        pozycje.Add(new PozycjaZamowienia(p, i));
    }

    public List<PozycjaZamowienia> Pobierz() => pozycje;

    public virtual decimal Suma(KalkulatorCeny k)
    {
        return k.Oblicz(pozycje);
    }

    public abstract string Info(KalkulatorCeny k);

    public virtual int CzasOczekiwania()
    {
        return 15 + pozycje.Sum(x => x.Ilosc);
    }

    public void ZmienStatus(StatusZamowienia s)
    {
        Status = s;
    }
}

public class ZamowienieNaMiejscu : Zamowienie
{
    public int Stolik { get; set; }

    public ZamowienieNaMiejscu(int s)
    {
        Stolik = s;
    }

    public override int CzasOczekiwania()
    {
        return base.CzasOczekiwania() - 5;
    }

    public override string Info(KalkulatorCeny k)
    {
        return $"NA MIEJSCU\nStolik: {Stolik}\nStatus: {Status}\nCzas: {CzasOczekiwania()} min\nSuma: {Suma(k):F2} zł";
    }
}

public class ZamowienieWDowozie : Zamowienie
{
    public string Adres { get; set; }
    public decimal Dostawa = 12.5m;

    public ZamowienieWDowozie(string a)
    {
        Adres = a;
    }

    public override int CzasOczekiwania()
    {
        return base.CzasOczekiwania() + 10;
    }

    public override decimal Suma(KalkulatorCeny k)
    {
        return base.Suma(k) + Dostawa;
    }

    public override string Info(KalkulatorCeny k)
    {
        return $"DOWÓZ\nAdres: {Adres}\nStatus: {Status}\nCzas: {CzasOczekiwania()} min\nSuma: {Suma(k):F2} zł";
    }
}