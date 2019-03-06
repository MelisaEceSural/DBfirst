using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DbFirst_20190227
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        Northwind2Entities data = new Northwind2Entities();

        private void Form1_Load(object sender, EventArgs e)
        {
            //dataGridView1.DataSource = data.Urunler.Where(x => x.Sonlandi == false).ToList();

            getDb();
        }
        void getProducts()
        {
            dataGridView1.DataSource = (from urun in data.Urunler.Where(x => x.Sonlandi == false && x.HedefStokDuzeyi > 0)
                                        join kat in data.Kategoriler
                                        on urun.KategoriID equals kat.KategoriID
                                        join ted in data.Tedarikciler
                                        on urun.TedarikciID equals ted.TedarikciID
                                        select new
                                        {
                                            Id = urun.UrunID,
                                            Adi = urun.UrunAdi,
                                            Fiyat = urun.BirimFiyati,
                                            Stok = urun.HedefStokDuzeyi,
                                            kat.KategoriAdi,
                                            urun.KategoriID,
                                            ted.SirketAdi,
                                            urun.TedarikciID
                                        }).ToList();


            numIndirim.Value = 0;

        }

        void getProducts(string aranan)
        {
            dataGridView1.DataSource = (from urun in data.Urunler.Where(x => x.Sonlandi == false && x.HedefStokDuzeyi > 0)
                                        join kat in data.Kategoriler
                                        on urun.KategoriID equals kat.KategoriID
                                        join ted in data.Tedarikciler
                                        on urun.TedarikciID equals ted.TedarikciID
                                        select new
                                        {
                                            Id = urun.UrunID,
                                            Adi = urun.UrunAdi,
                                            Fiyat = urun.BirimFiyati,
                                            Stok = urun.HedefStokDuzeyi,
                                            kat.KategoriAdi,
                                            urun.KategoriID,
                                            ted.SirketAdi,
                                            urun.TedarikciID
                                        }).Where(x=>x.Adi.Contains(aranan)).ToList();


            numIndirim.Value = 0;

        }
        void getDb()
        {
            getProducts();



            cmbPersonel.DataSource = data.Personeller.Select(p => new
            {
                Personel = p.Adi + " " + p.SoyAdi,
                Id = p.PersonelID
            }).ToList();
            cmbPersonel.DisplayMember = "Personel";
            cmbPersonel.ValueMember = "Id";

            cmbMusteri.DataSource = data.Musteriler.Select(m => new
            {
                m.SirketAdi,
                m.MusteriID
            }).ToList();
            cmbMusteri.DisplayMember = "SirketAdi";
            cmbMusteri.ValueMember = "MusteriID";

            cmbNakliye.DataSource = data.Nakliyeciler.ToList();
            cmbNakliye.DisplayMember = "SirketAdi";
            cmbNakliye.ValueMember = "NakliyeciID";
        }

        private void btnEkle_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Lütfen bir ürün seçin", "UYARI", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            DataGridViewRow satir = dataGridView1.CurrentRow;

            numAdet.Maximum = (short)satir.Cells[3].Value;

            ListViewItem li = new ListViewItem();
            li.Tag = satir.Cells[0].Value;
            li.Text = satir.Cells[1].Value.ToString();
            li.SubItems.Add(satir.Cells[2].Value.ToString());
            // Adet ve indirimi elle giriyorduk
            li.SubItems.Add(numAdet.Value.ToString());
            li.SubItems.Add(numIndirim.Value.ToString());
            decimal tutar = Convert.ToDecimal(satir.Cells[2].Value.ToString()) * numAdet.Value * (1 - (numIndirim.Value / 100));
            li.SubItems.Add(tutar.ToString());

            foreach (ListViewItem item in lstSepet.Items)
            {
                if ((int)li.Tag == (int)item.Tag)
                {
                    MessageBox.Show($"{li.Text} adlı ürün sepette mevcut.");
                    return;
                }
            }


            lstSepet.Items.Add(li);
            numAdet.Value = 1;
            getProducts();
        }

        private void btnCikar_Click(object sender, EventArgs e)
        {
            if (lstSepet.SelectedItems.Count == 0) return;
            lstSepet.Items.Remove(lstSepet.SelectedItems[0]);
        }

        private void btnSiparisVer_Click(object sender, EventArgs e)
        {
            if (lstSepet.Items.Count < 1)
            {
                MessageBox.Show("Sepette hiç ürün yok.");
                return;
            }

            if (cmbMusteri.SelectedItem == null || cmbNakliye.SelectedItem == null || cmbPersonel.SelectedItem == null)
            {
                MessageBox.Show("Lütfen gerekli alanları doldurun.");
                return;
            }

            // ARTIK SATIŞ YAPABİLİRİM
            Satislar yeniSatis = new Satislar();
            yeniSatis.SatisTarihi = DateTime.Today;
            yeniSatis.PersonelID = (int)cmbPersonel.SelectedValue;
            yeniSatis.MusteriID = cmbMusteri.SelectedValue.ToString();
            yeniSatis.ShipVia = (int)cmbNakliye.SelectedValue;
            yeniSatis.SevkTarihi = DateTime.Today.AddDays(7);
            data.Satislar.Add(yeniSatis);
            //Master bilgileri girdik, sırada detay bilgiler var
            foreach (ListViewItem urun in lstSepet.Items)
            {
                Satis_Detaylari sd = new Satis_Detaylari();
                sd.SatisID = yeniSatis.SatisID;
                sd.UrunID = (int)urun.Tag;
                sd.BirimFiyati = decimal.Parse(urun.SubItems[1].Text);
                sd.Miktar = short.Parse(urun.SubItems[2].Text);
                sd.İndirim = float.Parse(urun.SubItems[3].Text) / 100;

                data.Satis_Detaylari.Add(sd);
            }
            if (data.SaveChanges() > 0)
            {
                MessageBox.Show($"{yeniSatis.SatisID} Id'li satış gerçekleşti.");
                lstSepet.Items.Clear();
            }
            else
                MessageBox.Show("HATA");

            getProducts();

        }

        private void txtAra_TextChanged(object sender, EventArgs e)
        {
            getProducts(txtAra.Text);
        }
    }
}
