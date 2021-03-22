using System;
using System.Collections.Generic;

namespace GosZakup.ParsClass
{
    class Purchase // закупка
    {
        public int id { get; set; }
        public string num_purhchase { get; set; } // номер закупки
        public string name_of_purchase { get; set; } // название закупки
        public DateTime start_date { get; set; } // начало закупки
        public DateTime end_date { get; set; } // дата завершения закупки
        public double cost { get; set; } // стоимость общая закупки
        public string contact { get; set; } // контактные данные по закупке
        public string link { get; set; } // ссылка на страницу
        public string type_of_purshase { get; set; } // тип закупок
        public string status { get; set; } //статус закупки

        public int ConsumerID { get; set; }
        public virtual Consumer Consumer { get; set; }

        public ICollection<Lot> Lots { get; set; }
        public Purchase()
        {
            Lots = new List<Lot>();
        }
    }
}
