using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Linq;

namespace piramida
{
    public class Uczestnik
    {
        public uint Id { get; set; }
        public uint Level { get; set; }
        public List<Uczestnik> Children { get; set; }
        public uint Balance { get; set; }
        public uint? ParentId { get; set; }
    }

    public class Przelew
    {
        public uint Id { get; set; }
        public uint Amount { get; set; }
    }

    class Program
    {
        public static void Main()
        {
            //load xml files from debug folder:
            XDocument piramida = XDocument.Load("piramida.xml");
            XDocument transakcje = XDocument.Load("przelewy.xml");

            //deserialize xml to objects:
            List<Przelew> przelewy = LoadTransfers(transakcje.Descendants("przelew"));
            List<Uczestnik> uczestnicy = LoadParticipants(piramida.Descendants("uczestnik").Elements("uczestnik"));

            //add root to the list:
            uczestnicy.AddRange(LoadParticipants(piramida.Descendants("piramida").Elements("uczestnik")));

            //sort by ascending Ids:
            var ucz = uczestnicy.OrderBy(x => x.Id).ToList();

            //account balance process:
            ProcessTransfers(przelewy, ucz);

            //console output:
            ucz.ForEach(x => Console.WriteLine(x.Id + " " + x.Level + " " + x.Children.Count + " " + x.Balance));
        }

        public static List<Uczestnik> LoadParticipants(IEnumerable<XElement> participants)
        {
            return participants.Select(x => new Uczestnik()
            {
                Id = Convert.ToUInt32(x.Attribute("id").Value),
                Children = LoadParticipants(x.Elements("uczestnik")),
                Level = Convert.ToUInt32(x.Ancestors("uczestnik").Count().ToString()),
                ParentId = x.Parent.FirstAttribute != null ? Convert.ToUInt32(x.Parent.FirstAttribute.Value) : null
            }).ToList();
        }

        public static List<Przelew> LoadTransfers(IEnumerable<XElement> transfers)
        {
            return transfers.Select(x => new Przelew()
            {
                Id = Convert.ToUInt32(x.Attribute("od").Value),
                Amount = Convert.ToUInt32(x.Attribute("kwota").Value)
            }).ToList();
        }

        public static List<Uczestnik> ProcessTransfers(List<Przelew> przelewy, List<Uczestnik> uczestnicy)
        {
            foreach(Przelew p in przelewy)
            {
                Uczestnik u = uczestnicy.Find(x => x.Id == p.Id);
                
                //if the founder is a sender - all goes to him
                if (u.ParentId == null)
                    u.Balance += p.Amount;
                else
                {
                    Uczestnik parent = uczestnicy.Find(x => x.Id == u.ParentId);

                    //if the founder is the direct parent to the sender - all goes to the founder
                    //else perform dividing (TransferHelper)
                    if (parent.Level == 0)
                        parent.Balance += p.Amount;
                    else
                    {
                        TransferHelper(parent, p.Amount, uczestnicy, parent.Level);
                    }
                }
            }

            return uczestnicy;
        }

        public static uint TransferHelper(Uczestnik uczestnik, uint cash, List<Uczestnik> uczestnicy, uint lvl)
        {
            if (uczestnik.Level != 0)
            {
                uint rest = TransferHelper(uczestnicy.Find(x => x.Id == uczestnik.ParentId), cash, uczestnicy, lvl);
                
                uint temp = rest;
                rest /= 2;
                temp -= rest;
                uczestnik.Balance += rest;

                //if we are at the direct parent level we give him also temporary amount         
                if (uczestnik.Level == lvl)
                    return uczestnik.Balance += temp;
                //else continue dividing
                else
                    return temp;
            }
            else
            {
                //incoming 1$ case:
                if (cash / 2 == 0)
                {
                    uczestnik.Balance += cash;
                    return 0;
                }
                //founder gets half (rounded down) of the incoming transfer and returns the rest
                else
                {
                    uczestnik.Balance += cash / 2;
                    return cash - cash / 2;
                }               
            }
        }
    }
}