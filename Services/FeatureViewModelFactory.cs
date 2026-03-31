using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.Services
{
    /// <summary>
    /// Registry-based factory for creating feature ViewModels.
    /// Maps FeatureType → ViewModel factory, eliminating the need for
    /// individual Func&lt;T&gt; dependencies in CreatorViewModel.
    /// </summary>
    public interface IFeatureViewModelFactory
    {
        /// <summary>
        /// Creates a new ViewModel instance for the given feature type.
        /// </summary>
        ObservableObject Create(FeatureType featureType);

        /// <summary>
        /// Returns true if the factory can create a ViewModel for the given feature type.
        /// </summary>
        bool CanCreate(FeatureType featureType);
    }

    /// <summary>
    /// Concrete implementation using a dictionary of factory functions.
    /// Registered once in DI; CreatorViewModel receives only this single dependency
    /// instead of 8 individual factory functions.
    /// </summary>
    public class FeatureViewModelFactory : IFeatureViewModelFactory
    {
        private readonly Dictionary<FeatureType, Func<ObservableObject>> _factories;

        public FeatureViewModelFactory(Dictionary<FeatureType, Func<ObservableObject>> factories)
        {
            _factories = factories ?? throw new ArgumentNullException(nameof(factories));
        }

        public ObservableObject Create(FeatureType featureType)
        {
            if (!_factories.TryGetValue(featureType, out var factory))
                throw new ArgumentException($"No factory registered for feature type: {featureType}");

            return factory();
        }

        public bool CanCreate(FeatureType featureType)
        {
            return _factories.ContainsKey(featureType);
        }
    }
}
