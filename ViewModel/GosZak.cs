﻿using AngleSharp.Html.Parser;
using GosZakup.ParsClass;
using Leaf.xNet;
using System;
using System.Linq;
using System.Windows;

namespace GosZakup
{
    class GosZak
    {
        public static string GetPage(string link)  // метод получения кода страницы 
        {
            HttpRequest request = new HttpRequest();
            request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            request.AddHeader("Accept-Encoding", "gzip, deflate");
            request.AddHeader("Accept-Language", "ru,en;q=0.9,gl;q=0.8");
            request.AddHeader("Host", "goszakupki.by");
            request.AddHeader("Referer", link);
            request.KeepAlive = true;
            request.UserAgent = Http.IEUserAgent();
            
            metka:
            try
            {
               string resp = request.Get(link).ToString();
               return resp;
            }
            catch (Exception)
            {
                goto metka;
            }
        }

        public static string Pars_Num_Page(string page)  // подсчет количества страниц для парсинга (количество обходов для цикла парсинга карточек закупок)
        {
            try
            {
                HtmlParser Hp = new HtmlParser();
                var doc = Hp.ParseDocument(page);
                string last_page = doc.QuerySelector("li.last").TextContent;
                return last_page;
            }
            catch (Exception)
            {
                MessageBox.Show("Ошибка получения исходного кода страницы");
                return "";
            }
        }

        public static int ParsPage(string page) // парсинг всей страницы закупки
        {
            using (UserContext db = new UserContext())
            {
                int mun = 0;
                int num_tags = 0;
                string full_addr = "";
                string main_Site = "https://goszakupki.by";

                HtmlParser Hpars = new HtmlParser();
                var doc = Hpars.ParseDocument(page); // получаем исходник страницы

                num_tags = doc.QuerySelector("table.table.table-hover > tbody").ChildElementCount; // узнаем количество записей на странице 

                for (int i = 1; i <= num_tags; i++)
                {
                    // общие переменные
                    string last_page = doc.QuerySelector($"#w0 > table > tbody > tr:nth-child({i}) > td:nth-child(2) > a").OuterHtml; // в первой скобке номер элемента на странице .. в цикле меняем именно его... вывод на странице 20 элементов.
                    string numb_of_pursase = doc.QuerySelector($"#w0 > table > tbody > tr:nth-child({i}) > td:nth-child(1)").InnerHtml; // вырезаем номер закупки
                    string main_status = doc.QuerySelector("#w0 > table > tbody > tr:nth-child(1) > td:nth-child(4) > span").InnerHtml;// статус с общей страницы карточек закупок
                    //===============================создаем ссылку на карточку =============а нужно ли это???==========
                    // составляем ссылку 
                    int indexOfChar = last_page.IndexOf("\"") + 1;//номер первого вхождения двойных ковычек
                    last_page = last_page.Substring(indexOfChar);
                    int NextindexOfChar = last_page.IndexOf("\"");
                    string temp = last_page.Substring(0, NextindexOfChar);
                    full_addr = main_Site + temp;// конкатенация и вывод полного адреса страницы-карточки закупки.

                    // ==================================================
                    // обработка карточки

                    string blank_kod = GetPage(full_addr);

                    if (blank_kod != "") // если ошибка доступа к странице то пропускаем .
                    {
                        HtmlParser blankpars = new HtmlParser();
                        var blank = blankpars.ParseDocument(blank_kod);

                        //=========================== ПАРСИНГ ДАННЫХ С КАРТОЧКИ В БАЗУ ДАННЫХ =====================================
                        //есть два типа карточек в организатором и без - различаются кодом страниц 
                        string flag = blank.QuerySelector("body > div.wrap > div > div:nth-child(4) > div > b").InnerHtml;
                        if (flag == "Сведения о заказчике")
                        {
                            ParsBlankPage(0, blank_kod, numb_of_pursase, main_status, full_addr);
                        }
                        else
                        {
                            ParsBlankPage(1, blank_kod, numb_of_pursase, main_status, full_addr);
                        }
                        mun++; // СЧИТАЕМ ОБРАБОТАННЫЕ КАРТОЧКИ
                    }
                }
            return mun;
            }
        }

        public static void ParsBlankPage(int alfa, string blank_kod, string numb_of_pursase, string main_status, string full_addr)
        {
            using (UserContext db = new UserContext())
            {
                HtmlParser blankpars = new HtmlParser();
                var blank = blankpars.ParseDocument(blank_kod);

                // создаем все объекты
                Consumer consumer = new Consumer(); // покупатель
                Purchase purchase = new Purchase(); // основная карточка - закупка 
                Lot lot = new Lot(); // лот

                //=========работаем с покупателем===================================

                consumer.name = blank.QuerySelector($"body > div.wrap > div > div:nth-child({4 + alfa}) > table > tbody > tr:nth-child(1) > td").InnerHtml;
                consumer.adress = blank.QuerySelector($"body > div.wrap > div > div:nth-child({4 + alfa}) > table > tbody > tr:nth-child(2) > td").InnerHtml;
                consumer.unp = blank.QuerySelector($"body > div.wrap > div > div:nth-child({4 + alfa}) > table > tbody > tr:nth-child(3) > td").InnerHtml;

                var unicCon = db.Consumers.FirstOrDefault(p => p.unp == consumer.unp); // исключаем прасинг дубликатов (при повторном парсинге)

                if (unicCon == null)
                {
                    db.Consumers.Add(consumer);
                    db.SaveChanges();
                }

                //===========работаем с типами закупок====================================
                purchase.type_of_purshase = blank.QuerySelector("body > div.wrap > div > div:nth-child(3) > table > tbody > tr:nth-child(1) > td").InnerHtml;

                //====================работаем со статусами==================================
                purchase.status = main_status;

                //=============== если основные таблицы не пишут в базу, то подбрасываем первичные ключи из этих баз, для связи =======
                var temp_coms_id = db.Consumers.FirstOrDefault(p => p.unp == consumer.unp); // подхватываем id организации, что бы отдать заказу

                //===========работаем с корточкой закупок====================================
                purchase.num_purhchase = numb_of_pursase;
                purchase.name_of_purchase = blank.QuerySelector("body > div.wrap > div > div:nth-child(3) > table > tbody > tr:nth-child(2) > td").InnerHtml;
                purchase.start_date = DateTime.Parse(blank.QuerySelector($"body > div.wrap > div > div:nth-child({5 + alfa}) > table > tbody > tr:nth-child(1) > td").InnerHtml);
                purchase.end_date = DateTime.Parse(blank.QuerySelector($"body > div.wrap > div > div:nth-child({5 + alfa}) > table > tbody > tr:nth-child(2) > td").InnerHtml);
                var cost = blank.QuerySelector($"body > div.wrap > div > div:nth-child({5 + alfa}) > table > tbody > tr:nth-child(3) > td").InnerHtml;
                
                double value; // переконвертируем строку с ценой в double
                if (cost.Contains("."))
                {
                    double.TryParse(string.Join("", cost.Where(c => char.IsDigit(c))), out value);
                    value /= 100.00;
                }
                else
                {
                    double.TryParse(string.Join("", cost.Where(c => char.IsDigit(c))), out value);
                }
                purchase.cost = value;
                purchase.contact = blank.QuerySelector($"body > div.wrap > div > div:nth-child(4) > table > tbody > tr:nth-child({4 - alfa}) > td").InnerHtml;
                purchase.link = full_addr;
                purchase.ConsumerID = temp_coms_id.id;

                var unicPursh = db.Purchases.FirstOrDefault(p => p.num_purhchase == purchase.num_purhchase); // исключаем прасинг дубликатов (при повторном парсинге)

                if (unicPursh == null)
                {
                    db.Purchases.Add(purchase);
                    try
                    {
                        db.SaveChanges();
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Ошибка записи в базу данных.");
                    }
                }

                //===========работаем с лотами====================================

                int num_lot = blank.QuerySelector("#lotsList > tbody").ChildElementCount;// парсим количество лотов

                for (int j = 1; j <= num_lot; j = j + 2)
                {
                    lot.numlot = Convert.ToInt32(blank.QuerySelector($"#lotsList > tbody > tr:nth-child({j}) > th").InnerHtml);
                    lot.description = blank.QuerySelector($"#lotsList > tbody > tr:nth-child({j}) > td.lot-description").InnerHtml;
                    lot.delivery_date = blank.QuerySelector($"#lot-inf-{lot.numlot} > td:nth-child(3) > ul:nth-child(1) > li:nth-child(1) > span").InnerHtml;
                    lot.place_of_delivery = blank.QuerySelector($"#lot-inf-{lot.numlot} > td:nth-child(3) > ul:nth-child(1) > li:nth-child(2) > span").InnerHtml;
                    lot.source_of_financ = blank.QuerySelector($"#lot-inf-{lot.numlot} > td:nth-child(3) > ul:nth-child(1) > li:nth-child(3) > span").InnerHtml;
                    lot.payment_method = blank.QuerySelector($"#lot-inf-{lot.numlot} > td:nth-child(3) > ul:nth-child(1) > li:nth-child(4) > span").InnerHtml;
                    lot.price_quantity = blank.QuerySelector($"#lotsList > tbody > tr:nth-child({j}) > td.lot-count-price").InnerHtml;
                    lot.lot_status = blank.QuerySelector($"#lotsList > tbody > tr:nth-child({j}) > td.lot-status > span").InnerHtml;
                    lot.purshaseID = purchase.id;

                    var tempOKRB = blank.QuerySelector($"#lot-inf-{lot.numlot} > td:nth-child(3) > ul:nth-child(1) > li:nth-child(5) > b").InnerHtml;
                    if (tempOKRB == "Код предмета закупки по ОКРБ:")
                    {
                        lot.kodOKRB = blank.QuerySelector($"#lot-inf-{lot.numlot} > td:nth-child(3) > ul:nth-child(1) > li:nth-child(5) > span").InnerHtml;
                    }
                    else
                    {
                        lot.kodOKRB = blank.QuerySelector($"#lot-inf-{lot.numlot} > td:nth-child(3) > ul:nth-child(1) > li:nth-child(6) > span").InnerHtml;
                    }

                    var unic = db.Lots.FirstOrDefault(p => p.description == lot.description);
                    if (unic == null)
                    {
                        db.Lots.Add(lot);
                        db.SaveChanges();
                    }
                }
            }
        }

        public static void CheckingStatus() // поверка статуса закупок
        {
            using (UserContext db = new UserContext())
            {
                string firstPage = "https://goszakupki.by/tenders/posted?TendersSearch%5Bnum%5D="; // статическая часть адреса сходной точки

                var listPurchases = db.Purchases.ToList();
                for (int i = 0; i < listPurchases.Count; i++) // выбираем из базы данных попорядку все записи
                {
                    var NumPursaseDB = listPurchases[i].num_purhchase.ToString();

                    string kod = GetPage(firstPage + NumPursaseDB); // получаем код страницы
                    
                    HtmlParser Hpars = new HtmlParser();
                    var doc = Hpars.ParseDocument(kod);

                    string numb_of_pursase = doc.QuerySelector("#w0 > table > tbody > tr > td:nth-child(1)").InnerHtml; // вырезаем номер закупки
                    string main_status = doc.QuerySelector("#w0 > table > tbody > tr > td:nth-child(4) > span").InnerHtml;// статус с общей страницы карточек закупок
                    
                    if (main_status != listPurchases[i].status)
                    {
                        int idPurchase = listPurchases[i].id;
                        var p1 = db.Purchases.FirstOrDefault(p=>p.id == idPurchase);
                        p1.status = main_status;
                        db.SaveChanges();
                    }
                }
            }
        }
    }
}
