﻿namespace WGemCombiner
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using static Globals;

    #region Public Enums
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "In this context, Generic makes more sense than None.")]
    [Flags]
    public enum GemColors
    {
        Generic = 0,
        Orange = 1,
        Yellow = 1 << 1,
        Black = 1 << 2,
        Red = 1 << 3,
        HitFarm = Black | Red,
        Mana = Orange | Black | Red,
        Kill = Yellow | Black | Red
    }
    #endregion

    /// <summary>The Gem class represents a single gem in a recipe. Conceptually, it's comparable to an equation in that it is either a base gem (see BaseGem.cs) or a combination of two components.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Not a collection in any meaningful way.")]
    public class Gem : IEnumerable<Gem>
    {
        #region Constructors
        public Gem(Gem gem1, Gem gem2)
        {
            ThrowNull(gem1, nameof(gem1));
            ThrowNull(gem2, nameof(gem2));
            if (gem2.Cost > gem1.Cost || (gem2.Cost == gem1.Cost && gem2.Color < gem1.Color))
            {
                this.Component1 = gem2;
                this.Component2 = gem1;
            }
            else
            {
                this.Component1 = gem1;
                this.Component2 = gem2;
            }

            foreach (var component in this)
            {
                this.Color |= component.Color;
                this.IsSpec |= component.IsSpec || component.Color != this.Color;
                component.UseCount++;
            }

            if (this.Component1.Grade == this.Component2.Grade)
            {
                this.Grade = this.Component1.Grade + 1;
                this.Damage = CombineCalc(this.Component1.Damage, this.Component2.Damage, 0.87, 0.71);
                this.Blood = CombineCalc(this.Component1.Blood, this.Component2.Blood, 0.78, 0.31);
                this.CriticalMultiplier = CombineCalc(this.Component1.CriticalMultiplier, this.Component2.CriticalMultiplier, 0.88, 0.5);
                this.Leech = CombineCalc(this.Component1.Leech, this.Component2.Leech, 0.88, 0.5);
            }
            else if (Math.Abs(this.Component1.Grade - this.Component2.Grade) == 1)
            {
                this.Grade = this.Component1.Grade > this.Component2.Grade ? this.Component1.Grade : this.Component2.Grade;
                this.Damage = CombineCalc(this.Component1.Damage, this.Component2.Damage, 0.86, 0.7);
                this.Blood = CombineCalc(this.Component1.Blood, this.Component2.Blood, 0.79, 0.29);
                this.CriticalMultiplier = CombineCalc(this.Component1.CriticalMultiplier, this.Component2.CriticalMultiplier, 0.88, 0.44);
                this.Leech = CombineCalc(this.Component1.Leech, this.Component2.Leech, 0.89, 0.44);
            }
            else
            {
                this.Grade = this.Component1.Grade > this.Component2.Grade ? this.Component1.Grade : this.Component2.Grade;
                this.Damage = CombineCalc(this.Component1.Damage, this.Component2.Damage, 0.85, 0.69);
                this.Blood = CombineCalc(this.Component1.Blood, this.Component2.Blood, 0.8, 0.27);
                this.CriticalMultiplier = CombineCalc(this.Component1.CriticalMultiplier, this.Component2.CriticalMultiplier, 0.88, 0.44);
                this.Leech = CombineCalc(this.Component1.Leech, this.Component2.Leech, 0.9, 0.38);
            }

            this.Damage = Math.Max(this.Damage, Math.Max(this.Component1.Damage, this.Component2.Damage));
            this.Cost = this.Component1.Cost + this.Component2.Cost;
            if (this.IsSpec)
            {
                this.Growth = this.Power / Math.Pow(this.Cost, this.Color.HasFlag(GemColors.Orange) ? 0.627216 : 1.414061);
            }
            else
            {
                this.Growth = Math.Log(this.Power, this.Cost);
            }

            this.IsPureUpgrade = this.Component1 == this.Component2 && this.Component1.IsPureUpgrade && this.Component2.IsPureUpgrade;
        }

        protected Gem()
        {
        }
        #endregion

        #region Public Properties
        public GemColors Color { get; protected set; }

        public Gem Component1 { get; }

        public Gem Component2 { get; }

        public double Cost { get; protected set; }

        public string DisplayInfo => string.Format(CultureInfo.CurrentCulture, "Grade:  +{0}\r\nCost:   x{1:g6}\r\n{2}: {3:0.0#####}\r\nPower:  x{4:g6}", this.Grade, this.Cost, this.IsSpec ? "SCoeff" : "Growth", this.Growth, this.Power);

        public int Grade { get; protected set; }

        public double Growth { get; }

        public bool IsNeeded => this.Slot == Combiner.NotSlotted && this.UseCount > 0; // This has the side-effect of also ruling out base gems automatically

        public bool IsSpec { get; protected set; }

        public virtual bool IsUpgrade => this.Component1 == this.Component2;

        public double Power
        {
            get
            {
                if (this.Color == GemColors.Red)
                {
                    return 0;
                }

                double power = 1;
                if (this.Color.HasFlag(GemColors.Black))
                {
                    power *= this.Blood;
                }

                if (this.Color.HasFlag(GemColors.Yellow))
                {
                    power *= this.Damage * this.CriticalMultiplier;

                    if (this.Color.HasFlag(GemColors.Black))
                    {
                        // blood is squared here
                        power *= this.Blood;
                    }
                }
                else if (this.Color.HasFlag(GemColors.Orange))
                {
                    power *= this.Leech;
                }

                return power;
            }
        }

        public int Slot { get; set; }

        public string SpecWord => this.IsSpec ? "Spec" : "Combine";

        public int UseCount { get; set; }

        public virtual bool IsPureUpgrade { get; } // Set in constructor rather than climbing through the entire tree at every call - better speed at the cost of a slight memory increase per gem
        #endregion

        #region Internal Static Properties
        internal static string GemInitializer { get; } = "oykmgbrh";
        #endregion

        #region Protected Properties
        protected double Blood { get; set; }

        protected double CriticalMultiplier { get; set; }

        protected double Damage { get; set; } // max damage

        protected double Leech { get; set; }

        protected virtual string PureRecipe => this.IsPureUpgrade ? (this.Grade + 1).ToString(CultureInfo.CurrentCulture) + this.Component1.PureRecipe[this.Component1.PureRecipe.Length - 1] : null; // TODO: Getting the last letter this way is fugly, maybe find something better? Could implement PureLetter as Components[0].PureLetter, which would only climb through a single branch
        #endregion

        #region Public Methods
        public IEnumerator<Gem> GetEnumerator()
        {
            yield return this.Component1;
            yield return this.Component2;
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public virtual string Recipe() => this.PureRecipe ?? "(" + this.Component1.Recipe() + "+" + this.Component2.Recipe() + ")";
        #endregion

        #region Public Override Methods
        public override string ToString() => string.Format(CultureInfo.CurrentCulture, "Grade {0} {1} {2} ({3:0000}/{4:0000})", this.Grade + 1, this.Color, this.SpecWord, this.Cost, this.Growth);
        #endregion

        #region Private Static Methods
        private static double CombineCalc(double value1, double value2, double multHigh, double multLow) => value1 > value2 ? (multHigh * value1) + (multLow * value2) : (multHigh * value2) + (multLow * value1);
        #endregion
    }
}