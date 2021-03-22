using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace GosZakup.View
{

    public partial class ViewParsing : Window
    {
        int num_page = 100; // всего страниц для парсинга.
        int cards = 0;// счетчик отработанных карточек
        int pages = 0; // счетчик страниц с карточками
        CancellationTokenSource cts;

        public ViewParsing()
        {
            InitializeComponent();
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            BTN_Ok.Visibility = Visibility.Visible;
            BTN_Ok.IsEnabled = false;
            pbStatus.IsIndeterminate = true;

            TB_name.Text = "Парсинг запущен, дождитесь окончания процесса.";

            string firstPage = "https://goszakupki.by/tenders/posted?page="; // входная точка для парсинга
           
            int num_of_potoks = Environment.ProcessorCount;//количество потоков парсинга
            int delta = num_page / num_of_potoks;// делим работу между потоками
            if (num_page % num_of_potoks != 0) // если нацело не делится, добавим еще 1 поток для остатка
            {
                ++num_of_potoks;
            }
            List<int> numbers = new List<int>();
            numbers.Insert(0, 1);

            for (int i = 1; i < num_of_potoks; i++)
            {
                numbers.Add(numbers[i - 1] + delta);
            }

            await Task.Run(() => Multiplying_tasks(firstPage, delta, numbers));

            MessageBox.Show($"Парсинг завершен.", "Внимание.", MessageBoxButton.OK, MessageBoxImage.Information);
            BTN_Ok.IsEnabled = true;
            BTN_Cancel.IsEnabled = true;
            pbStatus.IsIndeterminate = false;
        }

        public void Multiplying_tasks(string firstPage, int delta, List<int> l) // генерируем потоки с заданием
        {
            cts = new CancellationTokenSource();

            Task[] tasks = new Task[l.Count];
            
            for (int i = 0; i < l.Count; i++)
            {
                int start_page_num = l[i];
                int end_page_num = num_page;
                if (i != l.Count - 1)
                {
                    end_page_num = l[i] + delta - 1;
                }
                tasks[i] = new Task(() => Pars_Task(firstPage, start_page_num, end_page_num, cts.Token));
            }

            foreach (var item in tasks)
                item.Start();

            Task.WaitAll(tasks); // ожидаем завершения задач
        }

        public void Pars_Task(string page, int start_page_num, int end_page_num, CancellationToken token)    // в цикле парсим всю базу
        {
            for (int i = start_page_num; i <= end_page_num; i++)
            {
                
                if (token.IsCancellationRequested)
                {
                    break;
                }
                try
                {
                    //string code_page = "";
                    string evrytPage = page + i; // страница для парсинга адресов карточек

                    string code_page = GosZak.GetPage(evrytPage); // получаем исходный код

                    if (code_page != "")
                    {
                        int mun = GosZak.ParsPage(code_page); // парсинг

                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
                        {
                            cards += mun;
                            pages += 1;
                            online_status2.Text = $"Обработано карточек - {cards} ";
                            online_status1.Text = $"Обработано страниц {pages} из {num_page}";
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Сервер не доступен, ошибка 503 " + ex + "\n Для продолжения нажмите OK");
                }
            }
        }

        private void BTN_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BTN_Cancel_Thrd(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
            BTN_Cancel.Visibility = Visibility.Visible;
            BTN_Cancel.IsEnabled = false;
        }
    }
}
