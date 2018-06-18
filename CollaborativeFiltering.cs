using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeFiltering
{
    class Program
    {
        static void Main(string[] args)
        {
            var cf = new CF();

            cf.ProcessInput(Console.ReadLine);

            foreach (var q in cf.ExecuteAll<Query>())
            {
                Console.WriteLine(cf.ToString(q));
            }
        }
    }

    public class CF
    {
        List<List<double>> NormalizedUserItemMatrix;
        List<List<double>> NormalizedItemUserMatrix;

        public CF()
        {
            //UserItemMatrix = new double[NumberOfItems,NumberOfUsers];
            UserItemMatrix = new List<List<int>>();
            ItemUserMatrix = new List<List<int>>();

            UserRatingAverage = new List<double>();
            ItemRatingAverage = new List<double>();
            Queries = new List<Query>();

            //Input(Console.ReadLine);

            //NormalizedUserItemMatrix = Normalize(UserItemMatrix);
            //NormalizedItemUserMatrix = Normalize(ItemUserMatrix);

            //ExecuteAll(Queries);
            //foreach (var q in Queries)
            //{
            //    Console.WriteLine(ToString(Execute<Query>(q)));
            //}
        }

        private void Normalize()
        {
            NormalizedUserItemMatrix = Normalize(UserItemMatrix);
            NormalizedItemUserMatrix = Normalize(ItemUserMatrix);
        }

        private IEnumerable<double> ExecuteAll<T>(IEnumerable<T> queries)
            where T : Query
        {
            return queries.Select(Execute);
        }

        public IEnumerable<double> ExecuteAll<T>()
            where T : Query
        {
            return Queries.Select(Execute);
        }

        private double Execute<T>(T query)
            where T : Query
        {
            double? result; // = default;
            // za svaki upit treba ispisati vrijednost preporuke u zasebnoj liniji
            switch (query.T)
            {
                case AlgorithmType.ItemItem:
                    result = Algorithm(query.I - 1, query.J - 1, query.K, UserItemMatrix, NormalizedUserItemMatrix);
                    break;
                case AlgorithmType.UserUser:
                    result = Algorithm(query.J - 1, query.I - 1, query.K, ItemUserMatrix, NormalizedItemUserMatrix); // , UserRatingAverage
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unknown algorithm code");
            }

            return result ?? default(double);
        }

        public int NumberOfItems { get; set; }
        public int NumberOfUsers { get; set; }
        
        private int NumberOfQueries { get; set; }
        public List<Query> Queries { get; set; }

        public List<List<int>> UserItemMatrix { get; set; }
        public List<List<int>> ItemUserMatrix { get; set; }

        private List<double> UserRatingAverage { get; set; }
        private List<double> ItemRatingAverage { get; set; }

        private static char DEFAULT_SEPARATOR = ' ';
        private static string NOT_AVAILABLE = "X";

        

        private List<double> CalculateAverage(List<List<double>> data)
        {
            List<double> result = data.Select(a => a.Where(rating => rating != Rating.None).Average()).ToList();

            return result;
        }


        private List<List<double>> Normalize(List<List<int>> data)
        {
            int items = data.Count;
            int users = data[0].Count;

            bool Valid(int rating) => rating != Rating.None;

            // Normalizacija ocjena predmeta oduzimanjem prosjeka predmeta od svake ocjene
            var average = data.Select(a => a.Where(Valid).Average()).ToList();

            //data.ForEach(list => list.ForEach(d => d -= list.Average()));
            var matrix = InitMatrix<double>(items, users);
            //InitMatrix(items, users, data, (i, j) => data[i][j] - average[i], (i, j) => data[i][j] != Rating.None);

            for (int i = 0; i < items; i++)
            {
                for (int j = 0; j < users; j++)
                {
                    var rating = data[i][j];
                    if (Valid(rating))
                    {
                        // za svakog korisnika x dohvati ocjenu predmeta j
                        matrix[i][j] = rating - average[i];
                    }
                }
            }

            return matrix;
        }

        private List<T> Calculate<T>(int itemCoordinate, IReadOnlyList<IReadOnlyList<double>> data)
            where T : Item, new()
        {
            List<T> similarities = new List<T>();

            for (int i = 0; i < data.Count; i++)
            {
                double result;

                if (i != itemCoordinate)
                {
                    var up = 0d;
                    var sumOfOwnerRatings = 0d;
                    var sumOfOtherRatings = 0d;

                    // gore je suma umnožaka elementa (x,y) po pozicijama
                    for (int j = 0; j < data[0].Count; j++)
                    {
                        var other = data[i][j];
                        var owner = data[itemCoordinate][j];
                        up += owner * other;

                        sumOfOwnerRatings += Math.Pow(owner, 2d);
                        sumOfOtherRatings += Math.Pow(other, 2d);
                    }
                    result = up / Math.Sqrt(sumOfOtherRatings * sumOfOwnerRatings);
                }
                else
                {
                    // item je sam sebi sličan
                    result = 1d;
                }

                similarities.Add(new T
                {
                    Position = i,
                    Value = result
                });
            }

            return similarities.OrderByDescending(s => s.Value).ToList(); // TODO: descending? dohvati k elemenata s najvećom vrijednošću sličnosti
        }

        private double Recommend<T>(int itemCoordinate, int userCoordinate, int k, IReadOnlyList<Item> similarities, IReadOnlyList<IReadOnlyList<int>> data)
        {
            var taken = default(int);
            var resultSimilarities = default(double);
            var gradeMultipleSimilarities = default(double);

            foreach (var similarity in similarities)
            {
                // dohvati k elemenata s najvećom vrijednošću sličnosti
                if (taken == k)
                {
                    break;
                }

                if (similarity.Value > 0)
                {
                    // pozicija elementa, tako da možemo uzeti ocjenu tog predmeta iz originalne tablice za usera
                    var grade = data[similarity.Position][userCoordinate];
                    // ako je ocjena veća od nule & ako nismo na promatranom predmetu
                    if (grade > 0 && similarity.Position != itemCoordinate)
                    {
                        taken++;
                        resultSimilarities += similarity.Value;

                        // ako ocjena postoji, onda je uzmi u obzir
                        gradeMultipleSimilarities += grade * similarity.Value;
                    }
                }

            }

            return gradeMultipleSimilarities / resultSimilarities; // recommendation
        }

        /// <summary>
        /// PCC (Pearson Correlation Coefficient) = potrebno je od pojedinih ocjena oduzeti prosjek predmeta (item-item)
        /// odnosno oduzeti prosjek korisnika(user-user) te nad normaliziranim ocjenama izracunati cosine mjeru slicnosti.
        /// </summary>
        private double? Algorithm(int itemCoordinate, int userCoordinate, int k, List<List<int>> data, List<List<double>> normalizedData)
        {
            //int items = data.Count;
            //int users = data[0].Count;

            if (data.Any())
            {
                var similarities = Calculate<Item>(itemCoordinate, normalizedData); // ordered
                return Recommend<double>(itemCoordinate, userCoordinate, k, similarities, data);
            }

            return null;
        }


        private class Item
        {
            public int Position { get; set; }

            public double Value { get; set; }
        }

        public List<List<T>> InitMatrix<T>(int x, int y)
        {
            List<List<T>> matrix = new List<List<T>>();
            for (int i = 0; i < x; i++)
            {
                matrix.Add(InitVector<T>(y));
            }

            return matrix;
        }

        public void InitMatrix<T>(int x, int y, List<List<T>> matrix)
        {
            for (int i = 0; i < x; i++)
            {
                matrix.Add(new List<T>());

                InitVector(y, matrix[i], () => default(T));
            }
        }

        public void InitMatrix<T>(int x, int y, List<List<T>> matrix, Func<int, int, T> value, Func<int, int, bool> filter)
        {
            for (int i = 0; i < x; i++)
            {
                matrix.Add(new List<T>());

                for (int j = 0; j < y; j++)
                {
                    if (filter(i, j))
                    {
                        matrix[i].Add(value(i, j));
                    }
                }
                //InitVector(y, matrix[i], );
            }
        }

        public List<T> InitVector<T>(int x)
        {
            List<T> vector = new List<T>(x);
            for (int i = 0; i < x; i++)
            {
                vector.Add(default(T));
            }
            return vector;
        }

        public void InitVector<T>(int x, List<T> vector, Func<T> value)
        {
            for (int i = 0; i < x; i++)
            {
                vector.Add(value());
            }
        }

        public void ProcessInput(Func<string> generator)
        {
            // prva linija sadrzi broj stavki i broj korisnika
            var line = Single(generator).Split(DEFAULT_SEPARATOR);
            NumberOfItems = Convert.ToInt32(line.First());
            NumberOfUsers = Convert.ToInt32(line.Last());

            //Single(generator);

            // Zatim slijedi zapis user-item matrice 
            // u kojoj su vrijednosti koje nedostaju prikazane znakom X.
            // Zapis matrice cini N linija od kojih svaka linija sadrzi M vrijednosti odijeljenih praznim znakom.
            // Vrijednost u matrici mogu biti cijeli brojevi u rasponu od 1 do 5.
            // Ukoliko vrijednost matrice ne postoji, tada su elementi oznaceni s X.
            UserItemMatrix = InitMatrix<int>(NumberOfItems, NumberOfUsers);
            ItemUserMatrix = InitMatrix<int>(NumberOfUsers, NumberOfItems);
            //for (int i = 0; i < NumberOfItems; i++)
            //{
            //    UserItemMatrix.Add(new List<double>());
            //    ItemUserMatrix.Add(new List<double>());

            //    for (int j = 0; j < NumberOfUsers; j++)
            //    {
            //        UserItemMatrix[i].Add(0d);
            //        ItemUserMatrix[i].Add(0d);
            //    }
            //}
            //InitVector(NumberOfItems, ItemRatingAverage, () => default);
            //InitVector(NumberOfUsers, UserRatingAverage, () => default);

            for (int i = 0; i < NumberOfItems; i++)
            {
                // ratings
                line = Single(generator).Split(DEFAULT_SEPARATOR);

                for (int j = 0; j < NumberOfUsers; j++)
                {
                    var rating = line[j];

                    var value = rating == NOT_AVAILABLE ? Rating.None : Convert.ToInt32(rating);

                    UserItemMatrix[i][j] = value;
                    ItemUserMatrix[j][i] = value;
                }
            }

            // Nakon zapisa matrice, iduća linija u ulaznoj datoteci jest konstanta Q koja predstavlja broj upita
            NumberOfQueries = Convert.ToInt32(Single(generator)); //  (1 <= Q <= 100).
            for (int i = 0; i < NumberOfQueries; i++) // svaka linija jedan upit
            {
                //Upit čine 4 broja I, J, T i K koji su odijeljeni praznim znakovima.
                line = Single(generator).Split(DEFAULT_SEPARATOR);

                var query = new Query()
                {
                    I = Convert.ToInt32(line[0]),
                    J = Convert.ToInt32(line[1]),
                    T = (AlgorithmType)Convert.ToInt32(line[2]),
                    K = Convert.ToInt32(line[3])
                };

                Queries.Add(query);
            }

            Normalize();
        }

        static T Single<T>(Func<T> generator)
        {
            return generator();
        }

        static IEnumerable<T> Generate<T>(Func<T> generator)
        {
            while (true) yield return generator();
        }

        public string ToString(double value)
        {
            return Math.Round(value, 3, MidpointRounding.AwayFromZero).ToString("##.000", CultureInfo.InvariantCulture);
            //return $"{Math.Round(value, 3, MidpointRounding.AwayFromZero):##.000}, CultureInfo.InvariantCulture";
        }
    }

    public class Query
    {
        /// <summary>
        /// Broj I (1 &lt;= I &lt;= N) predstavlja jednu stavku u matrici
        /// <para>
        /// [I, J] => koordinate elementa matrice označenog znakom X
        /// => element za koji je potrebno izračunati vrijednost preporuke
        /// </para>
        /// </summary>
        public int I { get; set; }
        /// <summary>
        /// Broj J (1 &lt;= J &lt;= M) predstavlja jednog korisnika u matrici
        /// <para>
        /// [I, J] => koordinate elementa matrice označenog znakom X
        /// => element za koji je potrebno izračunati vrijednost preporuke
        /// </para>
        /// /// </summary>
        public int J { get; set; }
        /// <summary>
        /// Broj T određuje tip algoritma koji je potrebno koristiti
        /// <para>
        /// (T=0) => item-item pristup suradničkog filtriranja
        /// </para>
        /// <para>
        /// (T=1) => user-user pristup suradničkog filtriranja
        /// </para>
        /// </summary>
        public AlgorithmType T { get; set; }
        /// <summary>
        /// Broj K (1 &lt;= K &lt;= N, M) predstavlja max kardinalni broj skupa sličnih stavki/korisnika koje sustav preporuke razmatra prilikom računanja vrijednosti preporuka
        /// </summary>
        public int K { get; set; }
    }

    public class Rating
    {
        public static int None = 0;
    }

    public enum AlgorithmType
    {
        ItemItem = 0,
        UserUser = 1
    }
}
