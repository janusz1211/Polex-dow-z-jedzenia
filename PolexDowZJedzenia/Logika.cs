using System;
using System.Collections.Generic;
using System.IO;
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
    public string Nazwa { get; init; }
    public decimal Cena { get; init; }
    public KategoriaProduktu Kategoria { get; init; }
    public decimal ProcentPrzeceny { get; init; }

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
    public Produkt Produkt { get; init; }
    public int Ilosc { get; init; }

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
public record WynikObliczen(decimal FinalnaCena, bool UzytoZestawu);

public class RegulaCenowa
{
    public WynikObliczen Oblicz(IReadOnlyList<PozycjaZamowienia> p, decimal suma)
    {
        bool danie = p.Any(x => x.Produkt.Kategoria == KategoriaProduktu.DanieGlowne);
        bool napoj = p.Any(x => x.Produkt.Kategoria == KategoriaProduktu.Napoj);
        bool deser = p.Any(x => x.Produkt.Kategoria == KategoriaProduktu.Deser);

        decimal znizkaZestaw = 0;
        bool uzytoZestawu = false;

        if (danie && napoj && deser)
        {
            znizkaZestaw = suma * 0.10m;
            uzytoZestawu = true;
        }

        decimal znizkaProduktowa = p.Sum(x => x.Produkt.Oszczednosc() * x.Ilosc);
        decimal ostatecznaCena = suma - Math.Max(znizkaZestaw, znizkaProduktowa);

        return new WynikObliczen(ostatecznaCena, uzytoZestawu);
    }
}

public class KalkulatorCeny
{
    private readonly RegulaCenowa r;
    private bool czyZestaw;

    public KalkulatorCeny(RegulaCenowa r)
    {
        this.r = r;
    }

    public decimal Oblicz(IReadOnlyList<PozycjaZamowienia> p)
    {
        decimal suma = p.Sum(x => x.WartoscBezRabatow());
        var wynik = r.Oblicz(p, suma);

        czyZestaw = wynik.UzytoZestawu;
        return wynik.FinalnaCena;
    }

    public bool CzyZestaw() => czyZestaw;
}

public abstract class Zamowienie
{
    private readonly List<PozycjaZamowienia> pozycje = new List<PozycjaZamowienia>();

    private static int licznik = 1;

    public int Id { get; }

    public StatusZamowienia Status { get; private set; }
    public DateTime Data { get; init; }

    protected Zamowienie()
    {
        Status = StatusZamowienia.Przyjete;
        Data = DateTime.Now;
        Id = licznik++;
    }

    public void Dodaj(Produkt p, int i)
    {
        if (p != null && i > 0)
        {
            pozycje.Add(new PozycjaZamowienia(p, i));
        }
    }

    public IReadOnlyList<PozycjaZamowienia> Pobierz() => pozycje.AsReadOnly();

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
    public int Stolik { get; init; }

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
    public string Adres { get; init; }
    public decimal Dostawa { get; } = 12.5m; 

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
        return $"DOWÓZ\nAdres: {Adres}\nID: {Id}\nData: {Data:yyyy-MM-dd HH:mm}\nStatus: {Status}\nCzas: {CzasOczekiwania()} min\nSuma: {Suma(k):F2} zł";
    }
}