using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;

namespace Atletika
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<Versenyzo> Versenyzok { get; } = new ObservableCollection<Versenyzo>();

        ICollectionView nezet;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            nezet = CollectionViewSource.GetDefaultView(Versenyzok);
            nezet.Filter = SzuroFeltetel;
        }

        private void Eletkor_Valtozott(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtEletkor != null)
            {
                txtEletkor.Text = $"{Convert.ToInt32(sldEletkor.Value)} év";
            }
        }

        private void Regisztral_Click(object sender, RoutedEventArgs e)
        {
            string nev = txtNev.Text.Trim();

            if (nev == "")
            {
                MessageBox.Show("A versenyző nevének megadása kötelező.", "Figyelmeztetés", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (nev.Contains(";") || nev.Contains("|"))
            {
                MessageBox.Show("A névben pontosvessző és függőleges vonal nem használható.", "Figyelmeztetés", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (cmbVersenyszam.SelectedItem == null)
            {
                MessageBox.Show("Válasszon versenyszámot.", "Figyelmeztetés", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (rbFerfi.IsChecked != true && rbNo.IsChecked != true)
            {
                MessageBox.Show("Adja meg a versenyző nemét.", "Figyelmeztetés", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int rajtszam = 0;

            if (txtRajtszam.Text.Trim() != "")
            {
                if (!int.TryParse(txtRajtszam.Text.Trim(), out rajtszam) || rajtszam < 1 || rajtszam > 9999)
                {
                    MessageBox.Show("A rajtszám 1 és 9999 közötti egész szám lehet.", "Figyelmeztetés", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool foglalt = false;
                foreach (Versenyzo v in Versenyzok)
                {
                    if (v.Rajtszam == rajtszam)
                    {
                        foglalt = true;
                    }
                }
                if (foglalt)
                {
                    MessageBox.Show("A kért rajtszám már foglalt. Adjon meg másikat, vagy hagyja üresen a mezőt.", "Figyelmeztetés", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                for (int i = 1; i <= 9999 && rajtszam == 0; i++)
                {
                    bool hasznalt = false;
                    foreach (Versenyzo v in Versenyzok)
                    {
                        if (v.Rajtszam == i)
                        {
                            hasznalt = true;
                        }
                    }
                    if (!hasznalt)
                    {
                        rajtszam = i;
                    }
                }

                if (rajtszam == 0)
                {
                    MessageBox.Show("Nincs több szabad rajtszám.", "Figyelmeztetés", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            string versenyszam = ((ComboBoxItem)cmbVersenyszam.SelectedItem).Content.ToString();
            string nem = rbFerfi.IsChecked == true ? "F" : "N";
            int eletkor = (int)sldEletkor.Value;
            bool or = chkOlimpiai.IsChecked == true;
            bool vr = chkVB.IsChecked == true;
            bool er = chkEB.IsChecked == true;

            Versenyzok.Add(new Versenyzo(rajtszam, nev, versenyszam, nem, eletkor, or, vr, er));

            nezet.Refresh();
            LetszamFrissit();

            MessageBox.Show($"A regisztráció sikeres. Kiosztott rajtszám: {rajtszam}", "Regisztráció", MessageBoxButton.OK, MessageBoxImage.Information);

            txtNev.Text = "";
            txtRajtszam.Text = "";
        }

        private bool SzuroFeltetel(object elem)
        {
            Versenyzo v = (Versenyzo)elem;

            if (cmbRekordSzuro.SelectedIndex == 1)
            {
                return v.OR;
            }
            if (cmbRekordSzuro.SelectedIndex == 2)
            {
                return v.VR;
            }
            if (cmbRekordSzuro.SelectedIndex == 3)
            {
                return v.ER;
            }
            return true;
        }

        private void Szuro_Valtozott(object sender, SelectionChangedEventArgs e)
        {
            if (nezet == null)
            {
                return;
            }

            nezet.Refresh();
            LetszamFrissit();
        }

        private void LetszamFrissit()
        {
            int lathato = 0;
            foreach (object elem in nezet)
            {
                lathato++;
            }
            txtLetszam.Text = $"Látható: {lathato} / Összesen: {Versenyzok.Count}";
        }

        private void Mentes_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "CSV-fájl (*.csv)|*.csv|Szövegfájl (*.txt)|*.txt";
            sfd.FilterIndex = 1;
            sfd.FileName = "atletika";
            sfd.AddExtension = false; 

            if (sfd.ShowDialog() != true)
            {
                return;
            }

            string valasztott = sfd.FilterIndex == 2 ? ".txt" : ".csv";
            string fajlnev = sfd.FileName;
            string kiterjesztes = System.IO.Path.GetExtension(fajlnev).ToLower();

            if (kiterjesztes == "")
            {
                fajlnev += valasztott;
            }
            else if (kiterjesztes != valasztott)
            {
                MessageBox.Show($"A kiválasztott fájltípus ({valasztott}) és a fájlnév kiterjesztése ({kiterjesztes}) nem egyezik. A mentés nem történt meg.", "Figyelmeztetés", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (StreamWriter sw = new StreamWriter(fajlnev, false, new UTF8Encoding(false)))
                {
                    if (valasztott == ".csv")
                    {
                        sw.WriteLine("Rajtszam;Nev;Versenyszam;Nem;Eletkor;OR;VR;ER");
                        foreach (Versenyzo v in Versenyzok)
                        {
                            sw.WriteLine($"{v.Rajtszam};{v.Nev};{v.Versenyszam};{v.Nem};{v.Eletkor};{(v.OR ? 1 : 0)};{(v.VR ? 1 : 0)};{(v.ER ? 1 : 0)}");
                        }
                    }
                    else
                    {
                        for (int i = 0; i < Versenyzok.Count; i++)
                        {
                            Versenyzo v = Versenyzok[i];

                            if (i > 0)
                            {
                                sw.WriteLine();
                            }

                            string nemSzoveg = v.Nem == "F" ? "férfi" : "női";
                            string orSzoveg = v.OR ? "Olimpiai csúccsal rendelkezik" : "Olimpiai csúccsal nem rendelkezik";
                            string vrSzoveg = v.VR ? "világcsúccsal rendelkezik" : "világcsúccsal nem rendelkezik";
                            string erSzoveg = v.ER ? "Európa-csúccsal rendelkezik" : "Európa-csúccsal nem rendelkezik";

                            sw.WriteLine(v.Nev);
                            sw.WriteLine($"A {v.Eletkor} éves {nemSzoveg} versenyző a(z) {v.Versenyszam} versenyszámban indul, rajtszáma {v.Rajtszam}. {orSzoveg}, {vrSzoveg}, {erSzoveg}.");
                        }
                    }
                }

                MessageBox.Show("A mentés sikeres.", "Mentés", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba a fájl mentésekor: " + ex.Message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Betoltes_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "CSV-fájl (*.csv)|*.csv";

            if (ofd.ShowDialog() != true)
            {
                return;
            }

            List<Versenyzo> ideiglenes = new List<Versenyzo>();

            try
            {
                string[] sorok = File.ReadAllLines(ofd.FileName, Encoding.UTF8);

                for (int i = 1; i < sorok.Length; i++)
                {
                    if (sorok[i].Trim() == "")
                    {
                        continue;
                    }

                    string[] m = sorok[i].Split(';');

                    Versenyzo uj = new Versenyzo(
                        int.Parse(m[0].Trim()),
                        m[1].Trim(),
                        m[2].Trim(),
                        m[3].Trim(),
                        int.Parse(m[4].Trim()),
                        m[5].Trim() == "1",
                        m[6].Trim() == "1",
                        m[7].Trim() == "1");

                    foreach (Versenyzo v in ideiglenes)
                    {
                        if (v.Rajtszam == uj.Rajtszam)
                        {
                            MessageBox.Show($"A fájlban többször szerepel a(z) {uj.Rajtszam} rajtszám. A betöltés nem történt meg, a korábbi adatok megmaradtak.", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    ideiglenes.Add(uj);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba a fájl beolvasásakor: " + ex.Message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Versenyzok.Clear();
            foreach (Versenyzo v in ideiglenes)
            {
                Versenyzok.Add(v);
            }

            nezet.Refresh();
            LetszamFrissit();
        }
    }
}