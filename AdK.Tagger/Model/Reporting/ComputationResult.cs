using System;

namespace AdK.Tagger.Model.Reporting
{
	public class ComputationResult : IComparable<ComputationResult>, IComparable
	{
		public decimal Value;
		public ComputationResult(decimal value)
		{
			Value = value;
		}

		public int CompareTo(ComputationResult other)
		{
			if (other != null)
				return Value.CompareTo(other.Value);
			return 0;
		}

		public int CompareTo(object obj)
		{
			if (obj is ComputationResult)
				return this.CompareTo(obj as ComputationResult);
			return 0;
		}

		public override string ToString()
		{
			return Value.ToString();
		}
	}
	public class Quantity : ComputationResult
	{
		public Quantity(int value) : base(value) { }

		public override string ToString()
		{
			return Value.ToString();
		}
	}
	public class Currency : ComputationResult
	{
		public Currency(decimal value) : base(value) { }

		public override string ToString()
		{
			return string.Format("{0:0.00}", Value);
			// return Value.ToString("C"); // Former currency formatting with currency unit
		}
	}
	public class Duration : ComputationResult
	{
		public Duration(decimal value) : base(value) { }

		public override string ToString()
		{
			return TimeSpan.FromSeconds((int)Math.Round(Value)).ToString("g");
		}
	}
	public class Percentage : ComputationResult
	{
		public Percentage(decimal value) : base(value) { }

		public override string ToString()
		{
			return string.Format("{0:0.#}%", Value * 100);
		}
	}
}