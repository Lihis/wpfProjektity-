﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;

namespace wpfProjektityö
{
    /// <summary>
    /// Interaction logic for LisääVarausWindow.xaml
    /// </summary>
    public partial class LisääVarausWindow : Window
    {
        string itemTag = null, varausPvm = null, valittuSaliID = null, asiakasID = null;
        List<int> valitutItemit = new List<int>();

        public LisääVarausWindow(ListBox list, ListBoxItem valittuSali)
        {
            InitializeComponent();

            // Ota päivämäärä ListBox:n tägistä
            varausPvm = list.Tag.ToString();

            // SaliID
            valittuSaliID = valittuSali.Tag.ToString();

            foreach (ListBoxItem valittu in list.SelectedItems)
            {
                // Hae Tag, tägissä on varausID jos on klikattu jo olemassa olevaa varausta
                if (valittu.Tag != null)
                    itemTag = valittu.Tag.ToString();

                // Lisää itemi listaan
                valitutItemit.Add(list.Items.IndexOf(valittu));
            }
        }

        // Sulje ikkuna kun painetaan "Peruuta"
        private void btnPeruuta_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Jos itemTag ei ole null, halutaan hakea jo olemassa olevan varauksen tiedot
            // muussa tapauksessa ollaan tekemässä uutta varausta
            if (itemTag != null)
            {
                // Muuta ikkunan teksti jos ollaan muokkaamassa varausta
                this.Title = "Muokkaa varausta";
                // Muuta napin teksti jos ollaan muokkaamassa varausta
                btnTeeVaraus.Content = "Tallenna";
                // Disable "Lisää varaus" -nappi
                //btnTeeVaraus.IsEnabled = false;
                haeVarauksenTiedot(itemTag);
            }
            else
            {
                cmbAlkuAika.SelectedIndex = valitutItemit.First();
                cmbLoppuAika.SelectedIndex = valitutItemit.Last();
                txtPvm.Text = varausPvm;
            }
        }

        // Hae varauksen ja asiakkaan tiedot "Lisää varaus"-ikkunaan
        void haeVarauksenTiedot(string varausID)
        {
            XmlReader reader = XmlReader.Create(@"Resources\varaukset.xml");

            // Hae varauksen tiedot
            while (reader.Read())
            {
                reader.MoveToContent();

                if (reader.NodeType == XmlNodeType.Element &&
                    reader.Name == "VarausID")
                {
                    // Lue kunnes ollaan halutun asiakkaan VarausID:n kohdalla
                    reader.Read();
                    if (reader.Value == varausID)
                    {
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                switch (reader.Name)
                                {
                                    // Päivämäärä
                                    case "Pvm":
                                        reader.Read();
                                        txtPvm.Text = reader.Value;
                                        break;
                                    // Päivämäärä
                                    case "AsiakasID":
                                        reader.Read();
                                        asiakasID = reader.Value;
                                        break;
                                    // Varauksen nimi ("otsikko")
                                    case "Nimi":
                                        reader.Read();
                                        txtVarauksenNimi.Text = reader.Value;
                                        break;
                                    // Varauksen alkuaika
                                    case "AlkuAika":
                                        reader.Read();
                                        cmbAlkuAika.SelectedIndex = positiot.haePositio(reader.Value);
                                        break;
                                    // Varauksen loppuaika
                                    case "LoppuAika":
                                        reader.Read();
                                        cmbLoppuAika.SelectedIndex = positiot.haePositio(reader.Value) - 1;
                                        break;
                                }
                            }
                            // Lopeta lukeminen kun saavutaan </varaus> lopetus tagiin
                            else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Varaus")
                            {
                                break;
                            }
                        }
                        // Asiakkaan tiedot haettu -> lopeta lukeminen
                        break;
                    }
                }
            }
            reader.Close();

            // Hae asiakkaan tiedot
            reader = XmlReader.Create(@"Resources\XMLasiakas.xml");
            while (reader.Read())
            {
                reader.MoveToContent();

                if (reader.NodeType == XmlNodeType.Element &&
                    reader.Name == "AID")
                {
                    // Lue kunnes ollaan halutun asiakkaan AID kohdalla
                    reader.Read();
                    if (reader.Value == asiakasID)
                    {
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                switch (reader.Name)
                                {
                                    case "Nimi":
                                        reader.Read();
                                        txtVaraajanNimi.Text = reader.Value;
                                        break;
                                    case "Osoite":
                                        reader.Read();
                                        txtPostiosoite.Text = reader.Value;
                                        break;
                                    case "PostNum":
                                        reader.Read();
                                        txtPostinumero.Text = reader.Value;
                                        break;
                                    case "Postitoimipaik":
                                        reader.Read();
                                        txtPostitoimipaikka.Text = reader.Value;
                                        break;
                                    case "Puh":
                                        reader.Read();
                                        txtPuhNro.Text = reader.Value;
                                        break;
                                    default:
                                        break;
                                }
                            }

                            // Lopeta lukeminen kun saavutaan </asiakas> lopetus tagiin
                            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "asiakas")
                            {
                                    break;
                            }
                        }
                        // Asiakkaan tiedot haettu -> lopeta lukeminen
                        break;
                    }
                }
            }
            reader.Close();
        }

        int lisääVaraus()
        {
            // Tarkista että varauksella on asiakas
            if (asiakasID == null)
            {
                MessageBox.Show("Lisää varaukselle asiakas!");
                return 1;
            }
            string varausID = null, saliID = null, pvm = null, nimi = null, alkuaika = null, loppuaika = null;
            int tmpVarausID = -1;

            // Uuden varauksen varausID:n hakeminen/laskeminen
            XmlReader reader = XmlReader.Create(@"Resources\varaukset.xml");
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "VarausID":
                            reader.Read();
                            int.TryParse(reader.Value, out tmpVarausID);
                            break;
                        default:
                            break;
                    }
                }
            }
            reader.Close();

            if (tmpVarausID < 0)
                return 2;
            // Lisää yksi varausID:n koska ollaan tekemässä uutta varausta
            varausID = (tmpVarausID + 1).ToString();

            // TODO: Tarkista että kentät ei ole tyhjiä
            saliID = valittuSaliID;
            pvm = txtPvm.Text;
            nimi = txtVarauksenNimi.Text;
            alkuaika = cmbAlkuAika.Text;
            loppuaika = cmbLoppuAika.Text;

            //// XML kirjoitus
            // XML lataus
            XmlDocument doc = new XmlDocument();
            doc.Load(@"Resources\varaukset.xml");
            XmlNode rootElement = doc.DocumentElement;

            // Uuden varaus elementin ja ali-elementtien luominen
            XmlElement varausElement = doc.CreateElement("Varaus");
            XmlElement elmVarausID = doc.CreateElement("VarausID");
            XmlElement elmSaliID = doc.CreateElement("SaliID");
            XmlElement elmAsiakasID = doc.CreateElement("AsiakasID");
            XmlElement elmPvm = doc.CreateElement("Pvm");
            XmlElement elmNimi = doc.CreateElement("Nimi");
            XmlElement elmAlkuAika = doc.CreateElement("AlkuAika");
            XmlElement elmLoppuAika = doc.CreateElement("LoppuAika");

            // Elementtien tekstit
            elmVarausID.InnerText = varausID;
            elmSaliID.InnerText = saliID;
            elmAsiakasID.InnerText = asiakasID;
            elmPvm.InnerText = pvm;
            elmNimi.InnerText = nimi;
            elmAlkuAika.InnerText = alkuaika;
            elmLoppuAika.InnerText = loppuaika;

            // Ali-elementit Varaus elementtiin
            varausElement.AppendChild(elmVarausID);
            varausElement.AppendChild(elmSaliID);
            varausElement.AppendChild(elmAsiakasID);
            varausElement.AppendChild(elmPvm);
            varausElement.AppendChild(elmNimi);
            varausElement.AppendChild(elmAlkuAika);
            varausElement.AppendChild(elmLoppuAika);

            // Lisää varaus element root elementtiin
            rootElement.AppendChild(varausElement);

            // XML Writerin avaaminen
            FileStream fileStream = new FileStream(@"Resources\varaukset.xml", FileMode.Create);
            StreamWriter streamWriter = new StreamWriter(fileStream);
            XmlTextWriter textWriter = new XmlTextWriter(streamWriter);
            textWriter.Formatting = Formatting.Indented;

            // XML tallennus
            doc.Save(textWriter);

            // XML writer sulkeminen
            textWriter.Close();
            streamWriter.Close();
            fileStream.Close();

            return 0;
        }

        int lisääAsiakas()
        {
            string asiakasID = null, nimi = null, osoite = null, postNum = null, postitoimipaik = null,
                email = null, puhNro = null, tyyppi = null;
            int tmpAsiakasID = -1;
            // Uuden asiakkaan AID:n hakeminen/laskeminen
            XmlReader reader = XmlReader.Create(@"Resources\XMLasiakas.xml");
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "VarausID":
                            reader.Read();
                            int.TryParse(reader.Value, out tmpAsiakasID);
                            break;
                        default:
                            break;
                    }
                }
            }
            reader.Close();
           
            if (tmpAsiakasID < 0)
                return 1;
            // Lisää yksi asikasID:n koska ollaan tekemässä uutta varausta
            asiakasID = (tmpAsiakasID + 1).ToString();

            nimi = txtVaraajanNimi.Text;
            osoite = txtPostiosoite.Text;
            postNum = txtPostinumero.Text;
            postitoimipaik = txtPostitoimipaikka.Text;
            // TODO: Email kenttä
            email = "asd@asd.fi";
            puhNro = txtPuhNro.Text;
            // TODO: Tyyppi kenttä
            tyyppi = "Yksityinen";

            //// XML kirjoitus
            // XML lataus
            XmlDocument doc = new XmlDocument();
            doc.Load(@"Resources\XMLasiakas.xml");
            XmlNode rootElement = doc.DocumentElement;

            // Uuden varaus elementin ja ali-elementtien luominen
            XmlElement asiakasElement = doc.CreateElement("asiakas");
            XmlElement elmAsiakasID = doc.CreateElement("AID");
            XmlElement elmNimi = doc.CreateElement("Nimi");
            XmlElement elmOsoite = doc.CreateElement("Osoite");
            XmlElement elmPostNro = doc.CreateElement("PostNum");
            XmlElement elmPostitoimipaik = doc.CreateElement("Postitoimipaik");
            XmlElement elmEmail = doc.CreateElement("Email");
            XmlElement elmPuh = doc.CreateElement("Puh");
            XmlElement elmTyyppi = doc.CreateElement("tyyppi");

            // Elementtien tekstit
            elmAsiakasID.InnerText = asiakasID;
            elmNimi.InnerText = nimi;
            elmOsoite.InnerText = osoite;
            elmPostNro.InnerText = postNum;
            elmPostitoimipaik.InnerText = postitoimipaik;
            elmEmail.InnerText = email;
            elmPuh.InnerText = puhNro;
            elmTyyppi.InnerText = tyyppi;

            // Ali-elementit Varaus elementtiin
            asiakasElement.AppendChild(elmAsiakasID);
            asiakasElement.AppendChild(elmNimi);
            asiakasElement.AppendChild(elmOsoite);
            asiakasElement.AppendChild(elmPostNro);
            asiakasElement.AppendChild(elmPostitoimipaik);
            asiakasElement.AppendChild(elmEmail);
            asiakasElement.AppendChild(elmPuh);
            asiakasElement.AppendChild(elmTyyppi);

            // Lisää varaus element root elementtiin
            rootElement.AppendChild(asiakasElement);

            // XML Writerin avaaminen
            FileStream fileStream = new FileStream(@"Resources\XMLasiakas.xml", FileMode.Create);
            StreamWriter streamWriter = new StreamWriter(fileStream);
            XmlTextWriter textWriter = new XmlTextWriter(streamWriter);
            textWriter.Formatting = Formatting.Indented;

            // XML tallennus
            doc.Save(textWriter);

            // XML writer sulkeminen
            textWriter.Close();
            streamWriter.Close();
            fileStream.Close();

            return 0;
        }

        // Varauksen lisääminen varaus.xml
        private void btnTeeVaraus_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Varauksen muokkaamisen implementointi?
            if (itemTag != null)
            {
                MessageBox.Show("Varauksen muokkausta ei ole implementoitu.");
                return;
            }

            // Lisää varaus XML:ään
            if (lisääVaraus() != 0)
            {
                MessageBox.Show("Virhe varausta lisätessä! Varausta ei tehty.");
            }
            else
            {
                MessageBox.Show("Varaus lisätty.");
            }
        }

        // Asiakkaan lisääminen XMLasiakas.xml:lään
        private void btnLisääAsiakas_Click(object sender, RoutedEventArgs e)
        {
            if (lisääAsiakas() != 0)
            {
                MessageBox.Show("Virhe asiakasta lisätessä! Asiakasta ei lisätty.");
            }
            else
            {
                MessageBox.Show("Asiakas lisätty.");
            }
        }

        private void btnHaeAsiaksTietokannasta_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Asiakkaan hakeminen nimellä
            //MessageBox.Show("Not implented yet");
            XmlReader reader = XmlReader.Create(@"Resources\XMLAsiakas.xml");
            string[] asiakkaanNimi = new string[] { "" };
            int asiakasLkm = 0;
            Regex pattern = new Regex(txtHaeVaraajanNimellä.Text.ToUpper());

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Nimi":
                            reader.Read();
                            if (pattern.IsMatch(reader.Value.ToUpper()))
                            {
                                asiakkaanNimi[asiakasLkm] = reader.Value;
                                asiakasLkm++;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            reader.Close();

            if (asiakkaanNimi.Length - 1 == 0)
            {
                MessageBox.Show("Asiakkaita ei löytynyt.");
            }
            else if (asiakkaanNimi.Length - 1 > 1)
            {
                string message = "Rajaa hakua, asiakkaita löytyi: " + asiakkaanNimi.Length.ToString() + " kpl.\n";

                for (int i = 0; i < asiakkaanNimi.Length; i++)
                {
                    message += asiakkaanNimi[i] + "\n";
                }
                MessageBox.Show(message);
            }
            else
            {
                // TODO: Asiakkaan tiedot laatikoihin
                MessageBox.Show("Asiakkaan nimi: " + asiakkaanNimi[0]);
            }
        }
    }
}
