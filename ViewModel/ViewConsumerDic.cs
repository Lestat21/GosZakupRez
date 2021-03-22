using System.Collections.Generic;
using System.Linq;

namespace GosZakup.ViewModel
{
    public class ViewConsumerDic
    {
        public string Unp { get; set; }
        public string Name { get; set; }
        public string Contact { get; set; }
        public string Adress { get; set; }

        public IEnumerable<List<ViewConsumerDic>> ConsumerDics() // создаем примежуточную таблицу для работы с вьюшкой программмы
        {
            UserContext db = new UserContext();
            List<ViewConsumerDic> viewConsumerDics = new List<ViewConsumerDic>();

            var result = from C in db.Consumers
                         join P in db.Purchases on C.id equals P.ConsumerID into cons
                         from supcons in cons.DefaultIfEmpty()

                         select new ViewConsumerDic
                         {
                             Unp = C.unp,
                             Name = C.name,
                             Adress = C.adress,
                             Contact = supcons.contact
                         };
            viewConsumerDics = result.ToList();

            var uniqueUsersList = viewConsumerDics.GroupBy(p => p.Unp, (key, g) => g.OrderBy(e => e.Unp).Take(1).ToList());

            return uniqueUsersList;
        }
    }
}
