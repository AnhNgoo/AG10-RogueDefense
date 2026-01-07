// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

using System;
using System.Collections.Generic;
using UnityEngine;

namespace InspectorGadgets
{
    /// <summary>
    /// A static stack that creates new elements as needed but keeps and reuses them instead of actually adding and
    /// removing like a regular stack.
    /// </summary>
    public abstract class StaticLazyStack<T> where T : StaticLazyStack<T>, new()
    {
        /************************************************************************************************************************/

        /// <summary>
        /// The elements managed by this class.
        /// <para></para>
        /// Note that the number of active elements is stored in <see cref="StackHeight"/> rather than the count of
        /// this list.
        /// </summary>
        /// <remarks>
        /// Not an actual Stack because that class is in System.dll which is not otherwise needed.
        /// </remarks>
        protected static readonly List<T> Stack = new List<T>();

        /// <summary>
        /// The number of elements currently active in the stack.
        /// </summary>
        public static int StackHeight { get; private set; }

        /************************************************************************************************************************/

        /// <summary>
        /// The maximum number of elements that can be in the stack at a time. Default = 10.
        /// </summary>
        public static int MaxHeight { get; set; }

        static StaticLazyStack()
        {
            MaxHeight = 10;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Increases the <see cref="StackHeight"/> by 1, ensures that an element exists at the last index and returns it.
        /// </summary>
        public static T Increment()
        {
            T item;

            if (StackHeight < Stack.Count)
            {
                item = Stack[StackHeight++];
            }
            else
            {
                item = new T();
                Stack.Add(item);
                StackHeight++;

                if (StackHeight > MaxHeight)
                    Debug.LogWarning($"StaticLazyStack<{typeof(T).Name}>.StackHeight" +
                        $" has exceeded the specified maximum of {MaxHeight}." +
                        $" Please ensure that you Remove as many items as you Add and increase the {nameof(MaxHeight)} if necessary.");
            }

            item.OnIncrement();
            return item;
        }

        /// <summary>Called by <see cref="Increment"/>.</summary>
        protected virtual void OnIncrement() { }

        /************************************************************************************************************************/

        /// <summary>Decreases the <see cref="StackHeight"/> by 1.</summary>
        public static void Decrement()
            => StackHeight--;

        /************************************************************************************************************************/

        /// <summary>Gets the element currently at the top of the stack.</summary>
        public static T GetCurrent()
            => StackHeight > 0 ? Stack[StackHeight - 1] : null;

        /************************************************************************************************************************/
    }

    /// <summary>
    /// A <see cref="StaticLazyStack{T}"/> which implements <see cref="IDisposable.Dispose"/> to automatically call
    /// <see cref="StaticLazyStack{T}.Decrement"/>
    /// </summary>
    public abstract class DisposableStaticLazyStack<T> : StaticLazyStack<T>, IDisposable where T : StaticLazyStack<T>, new()
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Calls <see cref="StaticLazyStack{T}.Decrement"/> and can be overridden.
        /// </summary>
        public virtual void Dispose()
        {
            Decrement();
        }

        /************************************************************************************************************************/
    }

    /// <summary>
    /// A <see cref="DisposableStaticLazyStack{T}"/> that gets and stores a particular <typeparamref name="TValue"/>,
    /// sets it to a different value, and then reverts it to the previous value when disposed.
    /// </summary>
    public abstract class SimpleStaticLazyStack<TStack, TValue> : DisposableStaticLazyStack<TStack>
        where TStack : SimpleStaticLazyStack<TStack, TValue>, new()
    {
        /************************************************************************************************************************/

        /// <summary>The <see cref="CurrentValue"/> from before this stack element was activated.</summary>
        public TValue PreviousValue { get; private set; }

        /// <summary>The <typeparamref name="TValue"/> being controlled by this stack.</summary>
        public abstract TValue CurrentValue { get; set; }

        /************************************************************************************************************************/

        /// <summary>
        /// Called by <see cref="StaticLazyStack{T}.Increment"/>.
        /// Stores the <see cref="CurrentValue"/> in <see cref="PreviousValue"/>.
        /// </summary>
        protected override void OnIncrement()
        {
            base.OnIncrement();
            PreviousValue = CurrentValue;
        }

        /// <summary>
        /// Calls <see cref="StaticLazyStack{T}.Decrement"/> and reverts the <see cref="CurrentValue"/> to the
        /// <see cref="PreviousValue"/>.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            CurrentValue = PreviousValue;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Increments the stack, sets the <see cref="CurrentValue"/>, and returns the top element of the stack.
        /// </summary>
        public static TStack Get(TValue value)
        {
            var context = Increment();
            context.CurrentValue = value;
            return context;
        }

        /************************************************************************************************************************/
    }
}

