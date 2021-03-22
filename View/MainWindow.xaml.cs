using GosZakup.ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GosZakup.View

{

    public partial class MainWindow : Window
    {
        UserContext db;

        public MainWindow()
        {
            InitializeComponent();
            db = new UserContext();

            try
            {
                var unic = db.Consumers.FirstOrDefault(p => p.id == 1); // проверяем бд на наличие данных  

                if (unic == null)
                {
                    MessageBox.Show($"Ваша база данных пуста.\nДля начала работы с программой необходимо загрузить данные с сайта Госзакупок. Это можно сделать через меню: Парсинг.", "Внимание!.", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DataLoad();
            }
            catch
            {
                MessageBox.Show("Отсутствует подключение к БД");
            }

            this.Closing += MainWindow_Closing; // чистим память
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            db.Dispose();
        }

        private void dGrid_LoadingRow(object sender, DataGridRowEventArgs e) // нумерация строк
        {
            e.Row.Header = e.Row.GetIndex() + 1;
        }

        private void Adout(object sender, RoutedEventArgs e) //Открываем окошко о программе
        {
            About about = new About();
            about.ShowDialog();
        }

        private void Close(object sender, RoutedEventArgs e)  // Заркыть программу
        {
            Close();
        }

        private void Del_Data(object sender, RoutedEventArgs e) // метод очистки базы данных + сбрасываем все индексы в нуль
        {
            var unic = db.Consumers.FirstOrDefault(p => p.id == 1);
            if (unic == null)
            {               
                    MessageBox.Show($"Ваша база данных пуста.\nОтсутствуют данные для удаления.", "Внимание!.", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBoxResult result = MessageBox.Show($"Продолжение операции приведет к уничтожению всех данных!", "Внимание.", MessageBoxButton.YesNo, MessageBoxImage.Warning);
               
                if (result == MessageBoxResult.Yes)
                {
                    db.Database.ExecuteSqlCommand(
                        "DELETE FROM Consumers " +
                        "DELETE FROM Purchases " +
                        "DELETE FROM Lots  " +
                        "DBCC CHECKIDENT ('consumers', RESEED, 0)" +
                        "DBCC CHECKIDENT ('Purchases', RESEED, 0)" +
                        "DBCC CHECKIDENT ('Lots', RESEED, 0)"
                        );
                    DataLoad();
                    MessageBox.Show($"База данных пуста!", "Внимание.", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                }
            }
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)  // просмотр карточки закупки и печать
        {
            DataGridRow row = sender as DataGridRow;

            if (row != null)
            {
                TextBlock tbl = MainTabl.Columns[0].GetCellContent(row) as TextBlock;
                Card card = new Card(tbl.Text);
                card.ShowDialog();
            }
        }

        private void DataLoad()  // загрузка из БД данных согласно полей таблицы в основную форму из виртуальной таблицы
        {
            MainViewTable MainViewTable = new MainViewTable();
            var res1 = from test in MainViewTable.MainVTable() select test;
            MainTabl.ItemsSource = res1.ToList();

            var type = db.Purchases.Select(p => p.type_of_purshase).Distinct();  // добавляем в комбобок список типов закупок
            List<string> list_of_type = new List<string>(type);
            CB.ItemsSource = list_of_type;

            var type2 = db.Purchases.Select(p => p.status).Distinct(); // добавляем в комбобокс статусы закупок
            List<string> list_of_stulis = new List<string>(type2);
            Status.ItemsSource = list_of_stulis;

            online_status1.Text = "Записей в таблице: " + res1.Count().ToString(); 
        }

        private void BC_Serch(object sender, RoutedEventArgs e) // реализация поиска по БД  
        {

            var unic = db.Consumers.FirstOrDefault(p => p.id == 1);
            if (unic == null)
            {
                MessageBox.Show($"Ваша база данных пуста.\nПоиск не возможен.", "Внимание!.", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MainViewTable MainViewTable = new MainViewTable();
                var result = from test in MainViewTable.MainVTable() select test;

                // приведение цены в формат для организации поиска 
                double start_price, end_price;
                double.TryParse(TB_PriceStart.Text, out start_price);
                double.TryParse(TB_PriceEnd.Text, out end_price);

                if (end_price == 0) // устанолвение макисмального значения конечной цены если она не указана в параметрах поиска
                {
                    end_price = Int32.MaxValue;
                }

                // работа с датой.. реализовано автозаполнение минимальным и максимальным значением если не выбраны
                if (DP_StartDate.SelectedDate != null && DP_EndDate.SelectedDate == null)
                {
                    DP_EndDate.SelectedDate = DateTime.MaxValue;
                }
                else if (DP_StartDate.SelectedDate == null && DP_EndDate.SelectedDate != null)
                {
                    DP_StartDate.SelectedDate = DateTime.MinValue;
                }
                else if (DP_StartDate.SelectedDate == null && DP_EndDate.SelectedDate == null)
                {
                    DP_EndDate.SelectedDate = DateTime.MaxValue;
                    DP_StartDate.SelectedDate = DateTime.MinValue;
                }

                // реализация поиска в виде запроса к промежуточной таблице с выбранными параметрами
                var serch = result.Where(p => p.unp.Contains(TB_UNP.Text))    //потом сделать поля из формы через проверку 
                                    .Where(p => p.name.ToLower().Contains(TB_Name.Text))
                                    .Where(p => p.name_of_purchase.ToLower().Contains(TB_NameOfPurshase.Text))
                                    .Where(p => p.type_of_purshase.Contains(CB.Text))
                                    .Where(p => p.status.Contains(Status.Text))
                                    .Where(p => p.cost >= start_price && p.cost <= end_price)
                                    .Where(p => p.start_date >= DP_StartDate.SelectedDate && p.end_date <= DP_EndDate.SelectedDate)
                                    .Select(c => c);

                online_status1.Text = "Записей в таблице: " + serch.Count().ToString();
                MainTabl.ItemsSource = serch.ToList();

                // обнуление параметров даты
                DP_EndDate.SelectedDate = DateTime.Now;
                DP_EndDate.SelectedDate = null;
                DP_StartDate.SelectedDate = DateTime.Now;
                DP_StartDate.SelectedDate = null;
            }
        }

        private void Bc_Clear(object sender, RoutedEventArgs e) // очистка формы
        {
            TB_Name.Text = "";
            TB_NameOfPurshase.Text = "";
            TB_Num.Text = "";
            TB_UNP.Text = "";
            TB_PriceStart.Text = "";
            TB_PriceEnd.Text = "";
            CB.SelectedItem = null;
            Status.SelectedItem = null;
            DP_EndDate.SelectedDate = null;
            DP_StartDate.SelectedDate = null;
            DataLoad();
        }

        private void ConsumersDic(object sender, RoutedEventArgs e)
        {
            ViewConsumerDic viewConsumerDic = new ViewConsumerDic();
            ConsumerDic consumerDic = new ConsumerDic(viewConsumerDic);
            consumerDic.ShowDialog();
        }

        private void DelInactiveCards(object sender, RoutedEventArgs e) // удаление завершенных/отмененных закупок
        {
            var unic = db.Consumers.FirstOrDefault(p => p.id == 1);
            if (unic == null)
            {
                MessageBox.Show($"Ваша база данных пуста.\nОтсутствуют данные для удаления.", "Внимание!.", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBoxResult result = MessageBox.Show($"Будут уделены все завершенные закупки!", "Внимание.", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    db.Database.ExecuteSqlCommand(
                        " DELETE FROM Lots FROM Lots join Purchases on lots.purshaseID = Purchases.id " +
                        " where Purchases.num_purhchase in (select Purchases.num_purhchase" +
                        " FROM Purchases" +
                        " where Purchases.status = 'Завершен' OR Purchases.status = 'Отменен' OR Purchases.status = 'Признан несостоявшимся')" +
                        " DELETE FROM Purchases" +
                        " where Purchases.status = 'Завершен' OR Purchases.status = 'Отменен' OR Purchases.status = 'Признан несостоявшимся'"
                        );
                    DataLoad();
                    MessageBox.Show($"Завершенные закупки удалены!", "Внимание.", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                }
            }
        }

        private async void Update_Status(object sender, RoutedEventArgs e) // обновление статусов закупки.
        {
            var unic = db.Consumers.FirstOrDefault(p => p.id == 1);
            if (unic == null)
            {
                MessageBox.Show($"Ваша база данных пуста.\nОтсутствуют записи для обновления.", "Внимание!.", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                M_Parser.IsEnabled = false;
                M_Update.IsEnabled = false;
                M_EraserDB.IsEnabled = false;
                M_DelOver.IsEnabled = false;
                online_status2.Text = "Идет обновление статусов";
                PB_Update.IsIndeterminate = true;
                await Task.Run(() => GosZak.CheckingStatus());
                MessageBox.Show("Обновление статусов завершено");
                PB_Update.IsIndeterminate = false;
                online_status2.Text = "";
                M_Parser.IsEnabled = true;
                M_Update.IsEnabled = true;
                M_EraserDB.IsEnabled = true;
                M_DelOver.IsEnabled = true;
                DataLoad();
            }
        }

        private void ViewParsing(object sender, RoutedEventArgs e)
        {
            ViewParsing viewParsing = new ViewParsing();
            viewParsing.ShowDialog();
            DataLoad();
        }
    }
}
