namespace Aisix.Common.Utils
{
    public class WeightedSelector<T>
    {
        private readonly Random _random = new Random();
        private readonly List<WeightedItem<T>> _items = new List<WeightedItem<T>>();

        public WeightedSelector()
        {
        }

        //public WeightedSelector(List<WeightedItem<T>> t)
        //{
        //    _items = t;
        //}

        public void AddItem(T item, double weight)
        {
            if (weight <= 0)
                throw new ArgumentException("Weight must be positive.", nameof(weight));

            _items.Add(new WeightedItem<T> { Item = item, Weight = weight });
        }

        public T SelectItem()
        {
            double totalWeight = _items.Sum(x => x.Weight);
            double randomValue = _random.NextDouble() * totalWeight;

            foreach (var item in _items)
            {
                if (randomValue <= item.Weight)
                    return item.Item;

                randomValue -= item.Weight;
            }

            // In theory, we shouldn't get here.
            throw new InvalidOperationException("Failed to select an item. This might be a logical error.");
        }

        public class WeightedItem<U>
        {
            public required U Item { get; set; }
            public double Weight { get; set; }
        }
    }

}
