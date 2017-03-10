﻿namespace Ana.Source.Snapshots
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Utils.Extensions;

    /// <summary>
    /// Defines a reference to an element within a snapshot region.
    /// </summary>
    internal class SnapshotElementRef
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotElementRef" /> class.
        /// </summary>
        /// <param name="parent">The parent region that contains this element.</param>
        /// <param name="index">The index of the element to begin pointing to.</param>
        public SnapshotElementRef(SnapshotRegion parent, Int32 index = 0)
        {
            this.Parent = parent;

            // The garbage collector can relocate variables at runtime. Since we use unsafe pointers, we need to keep these pinned
            this.CurrentValuesHandle = GCHandle.Alloc(this.Parent.CurrentValues, GCHandleType.Pinned);
            this.PreviousValuesHandle = GCHandle.Alloc(this.Parent.PreviousValues, GCHandleType.Pinned);

            this.InitializePointers(index);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="SnapshotElementRef" /> class.
        /// </summary>
        ~SnapshotElementRef()
        {
            // Let the GC do what it wants now
            this.CurrentValuesHandle.Free();
            this.PreviousValuesHandle.Free();
        }

        /// <summary>
        /// Gets the base address of this element.
        /// </summary>
        public IntPtr BaseAddress
        {
            get
            {
                return this.Parent.BaseAddress.Add(this.CurrentElementIndex);
            }
        }

        /// <summary>
        /// Gets or sets the label associated with this element.
        /// </summary>
        public Object ElementLabel
        {
            get
            {
                return this.Parent.ElementLabels[this.CurrentElementIndex];
            }

            set
            {
                this.Parent.ElementLabels[this.CurrentElementIndex] = value;
            }
        }

        /// <summary>
        /// Gets or sets a garbage collector handle to the current value array.
        /// </summary>
        private GCHandle CurrentValuesHandle { get; set; }

        /// <summary>
        /// Gets or sets a garbage collector handle to the previous value array.
        /// </summary>
        private GCHandle PreviousValuesHandle { get; set; }

        /// <summary>
        /// Gets or sets the parent snapshot region.
        /// </summary>
        private SnapshotRegion Parent { get; set; }

        /// <summary>
        /// Gets or sets the pointer to the current value.
        /// </summary>
        private unsafe Byte* CurrentValuePointer { get; set; }

        /// <summary>
        /// Gets or sets the pointer to the previous value.
        /// </summary>
        private unsafe Byte* PreviousValuePointer { get; set; }

        /// <summary>
        /// Gets or sets the index of this element, used for setting and getting the label.
        /// Note that we cannot have a pointer to the label, as it is a non-blittable type.
        /// </summary>
        private Int32 CurrentElementIndex { get; set; }

        /// <summary>
        /// Gets or sets the type code associated with the data type of this element.
        /// </summary>
        private TypeCode CurrentTypeCode { get; set; }

        /// <summary>
        /// Increments all value and label pointers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void IncrementPointers()
        {
            this.CurrentElementIndex++;
            this.CurrentValuePointer++;
            this.PreviousValuePointer++;
        }

        /// <summary>
        /// Increments all value and label pointers by the given alignment.
        /// </summary>
        /// <param name="alignment">The alignment by which to increment.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void AddPointers(Int32 alignment)
        {
            this.CurrentElementIndex += alignment;
            this.CurrentValuePointer += alignment;
            this.PreviousValuePointer += alignment;
        }

        /// <summary>
        /// Increments all value pointers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void IncrementPointersValuesOnly()
        {
            this.CurrentValuePointer++;
            this.PreviousValuePointer++;
        }

        /// <summary>
        /// Increments all value pointers by the given alignment.
        /// </summary>
        /// <param name="alignment">The alignment by which to increment.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void AddPointersValuesOnly(Int32 alignment)
        {
            this.CurrentValuePointer += alignment;
            this.PreviousValuePointer += alignment;
        }

        /// <summary>
        /// Increments all label pointers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void IncrementPointersLabelsOnly()
        {
            this.CurrentElementIndex++;
        }

        /// <summary>
        /// Increments all label pointers by the given alignment.
        /// </summary>
        /// <param name="alignment">The alignment by which to increment.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void AddPointersLabelsOnly(Int32 alignment)
        {
            this.CurrentElementIndex += alignment;
        }

        /// <summary>
        /// Increments all value and label pointers, except for previous values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void IncrementPointersNoPrevious()
        {
            this.CurrentElementIndex++;
            this.CurrentValuePointer++;
        }

        /// <summary>
        /// Increments all value and label pointers by the given alignment, except for previous values.
        /// </summary>
        /// <param name="alignment">The alignment by which to increment.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void AddPointersNoPrevious(Int32 alignment)
        {
            this.CurrentElementIndex += alignment;
            this.CurrentValuePointer += alignment;
        }

        /// <summary>
        /// Increments the current value pointer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void IncrementPointersCurrentValueOnly()
        {
            this.CurrentValuePointer++;
        }

        /// <summary>
        /// Increments the current value pointer by the given alignment.
        /// </summary>
        /// <param name="alignment">The alignment by which to increment.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void AddPointersCurrentValueOnly(Int32 alignment)
        {
            this.CurrentValuePointer += alignment;
        }

        /// <summary>
        /// Sets the valid bit of this element.
        /// </summary>
        /// <param name="isValid">Whether or not this element's valid bit is set.</param>
        public void SetValid(Boolean isValid)
        {
            this.Parent.GetValidBits().Set(this.CurrentElementIndex, isValid);
        }

        /// <summary>
        /// Determines if this element has changed.
        /// </summary>
        /// <returns>True if the element changed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Boolean Changed()
        {
            return this.LoadValue(this.CurrentValuePointer) != this.LoadValue(this.PreviousValuePointer);
        }

        /// <summary>
        /// Determines if this element has not changed.
        /// </summary>
        /// <returns>True if the element is unchanged.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Boolean Unchanged()
        {
            return this.LoadValue(this.CurrentValuePointer) == this.LoadValue(this.PreviousValuePointer);
        }

        /// <summary>
        /// Determines if this element has increased.
        /// </summary>
        /// <returns>True if the element increased.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Boolean Increased()
        {
            return this.LoadValue(this.CurrentValuePointer) > this.LoadValue(this.PreviousValuePointer);
        }

        /// <summary>
        /// Determines if this element has decreased.
        /// </summary>
        /// <returns>True if the element decreased.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Boolean Decreased()
        {
            return this.LoadValue(this.CurrentValuePointer) < this.LoadValue(this.PreviousValuePointer);
        }

        /// <summary>
        /// Determines if this element has a value equal to the given value.
        /// </summary>
        /// <param name="value">The value being compared against.</param>
        /// <returns>True if the values are equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Boolean EqualToValue(dynamic value)
        {
            return this.LoadValue(this.CurrentValuePointer) == value;
        }

        /// <summary>
        /// Determines if this element has a value not equal to the given value.
        /// </summary>
        /// <param name="value">The value being compared against.</param>
        /// <returns>True if the values are not equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Boolean NotEqualToValue(dynamic value)
        {
            return this.LoadValue(this.CurrentValuePointer) != value;
        }

        /// <summary>
        /// Determines if this element has a value greater than to the given value.
        /// </summary>
        /// <param name="value">The value being compared against.</param>
        /// <returns>True if the element value is greater than the given value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Boolean GreaterThanValue(dynamic value)
        {
            return this.LoadValue(this.CurrentValuePointer) > value;
        }

        /// <summary>
        /// Determines if this element has a value greater than or equal to the given value.
        /// </summary>
        /// <param name="value">The value being compared against.</param>
        /// <returns>True if the element value is greater than or equal the given value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Boolean GreaterThanOrEqualToValue(dynamic value)
        {
            return this.LoadValue(this.CurrentValuePointer) >= value;
        }

        /// <summary>
        /// Determines if this element has a value less than to the given value.
        /// </summary>
        /// <param name="value">The value being compared against.</param>
        /// <returns>True if the element value is less than the given value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Boolean LessThanValue(dynamic value)
        {
            return this.LoadValue(this.CurrentValuePointer) < value;
        }

        /// <summary>
        /// Determines if this element has a value less than or equal to the given value.
        /// </summary>
        /// <param name="value">The value being compared against.</param>
        /// <returns>True if the element value is less than or equal to the given value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Boolean LessThanOrEqualToValue(dynamic value)
        {
            return this.LoadValue(this.CurrentValuePointer) <= value;
        }

        /// <summary>
        /// Determines if the element has increased it's value by the given value
        /// </summary>
        /// <param name="value">The value being compared against.</param>
        /// <returns>True if the element value has increased by the given value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Boolean IncreasedByValue(dynamic value)
        {
            return this.LoadValue(this.CurrentValuePointer) == unchecked(this.LoadValue(this.PreviousValuePointer) + value);
        }

        /// <summary>
        /// Determines if the element has decreased it's value by the given value
        /// </summary>
        /// <param name="value">The value being compared against.</param>
        /// <returns>True if the element value has decreased by the given value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Boolean DecreasedByValue(dynamic value)
        {
            return this.LoadValue(this.CurrentValuePointer) == unchecked(this.LoadValue(this.PreviousValuePointer) - value);
        }

        /// <summary>
        /// Determines if the value is in scientific notation. Only applicable for Single and Double data types.
        /// </summary>
        /// <returns>True if the element is in scientific notation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Boolean IsScientificNotation()
        {
            return this.LoadValue(this.CurrentValuePointer).ToString().ToLower().Contains("e");
        }

        /// <summary>
        /// Gets the current value of this element.
        /// </summary>
        /// <returns>The current value of this element.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe dynamic GetCurrentValue()
        {
            return this.LoadValue(this.CurrentValuePointer);
        }

        /// <summary>
        /// Gets the previous value of this element.
        /// </summary>
        /// <returns>The previous value of this element.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe dynamic GetPreviousValue()
        {
            return this.LoadValue(this.PreviousValuePointer);
        }

        /// <summary>
        /// Gets the label of this element.
        /// </summary>
        /// <returns>The label of this element.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe dynamic GetElementLabel()
        {
            return this.Parent.ElementLabels == null ? null : this.Parent.ElementLabels[this.CurrentElementIndex];
        }

        /// <summary>
        /// Sets the label of this element.
        /// </summary>
        /// <param name="newLabel">The new element label.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void SetElementLabel(dynamic newLabel)
        {
            this.Parent.ElementLabels[this.CurrentElementIndex] = newLabel;
        }

        /// <summary>
        /// Determines if this element has a current value associated with it.
        /// </summary>
        /// <returns>True if a current value is present.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Boolean HasCurrentValue()
        {
            if (this.CurrentValuePointer == (Byte*)0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if this element has a previous value associated with it.
        /// </summary>
        /// <returns>True if a previous value is present.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Boolean HasPreviousValue()
        {
            if (this.PreviousValuePointer == (Byte*)0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Initializes snapshot value reference pointers
        /// </summary>
        /// <param name="index">The index of the element to begin pointing to.</param>
        private unsafe void InitializePointers(Int32 index = 0)
        {
            this.CurrentElementIndex = index;
            this.CurrentTypeCode = Type.GetTypeCode(this.Parent.ElementType);

            if (this.Parent.CurrentValues != null && this.Parent.CurrentValues.Length > 0)
            {
                fixed (Byte* pointerBase = &this.Parent.CurrentValues[index])
                {
                    this.CurrentValuePointer = pointerBase;
                }
            }
            else
            {
                this.CurrentValuePointer = null;
            }

            if (this.Parent.PreviousValues != null && this.Parent.PreviousValues.Length > 0)
            {
                fixed (Byte* pointerBase = &this.Parent.PreviousValues[index])
                {
                    this.PreviousValuePointer = pointerBase;
                }
            }
            else
            {
                this.PreviousValuePointer = null;
            }
        }

        /// <summary>
        /// Loads the value of this snapshot element from the given array.
        /// </summary>
        /// <param name="array">The byte array from which to read a value.</param>
        /// <returns>The value at the start of this array casted as the proper data type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe dynamic LoadValue(Byte* array)
        {
            switch (this.CurrentTypeCode)
            {
                case TypeCode.Byte:
                    return *array;
                case TypeCode.SByte:
                    return *(SByte*)array;
                case TypeCode.Int16:
                    return *(Int16*)array;
                case TypeCode.Int32:
                    return *(Int32*)array;
                case TypeCode.Int64:
                    return *(Int64*)array;
                case TypeCode.UInt16:
                    return *(UInt16*)array;
                case TypeCode.UInt32:
                    return *(UInt32*)array;
                case TypeCode.UInt64:
                    return *(UInt64*)array;
                case TypeCode.Single:
                    return *(Single*)array;
                case TypeCode.Double:
                    return *(Double*)array;
                default:
                    throw new Exception("Invalid element type");
            }
        }
    }
    //// End class
}
//// End namespace