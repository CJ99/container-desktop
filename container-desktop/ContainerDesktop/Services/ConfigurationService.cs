﻿using ContainerDesktop.Common;
using KellermanSoftware.CompareNetObjects;
using Newtonsoft.Json;
using System.IO.Abstractions;

namespace ContainerDesktop.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly string _configurationFilePath;
    private readonly IFileSystem _fileSystem;
    private readonly IProductInformation _productInformation;
    private ContainerDesktopConfiguration _loadedConfiguration;
    private readonly CompareLogic _comparer;

    public ConfigurationService(IFileSystem fileSystem, IProductInformation productInformation)
    {
        _comparer = new CompareLogic(new ComparisonConfig {  MaxDifferences = int.MaxValue});
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _productInformation = productInformation ?? throw new ArgumentNullException(nameof(productInformation));
        _configurationFilePath = Path.Combine(productInformation.ContainerDesktopAppDataDir, "config.json");
        Configuration = new ContainerDesktopConfiguration(productInformation);
        if (_fileSystem.File.Exists(_configurationFilePath))
        {
            Load(false);
        }
        else
        {
            Save(false);
        }
    }

    public bool IsChanged() => !_comparer.Compare(_loadedConfiguration, Configuration).AreEqual;
    
    public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;

    public ContainerDesktopConfiguration Configuration { get; }

    public void Save() => Save(true);

    public void Load() => Load(true);

    protected void Save(bool notify)
    {
        var result = _comparer.Compare(_loadedConfiguration, Configuration);
        if(!result.AreEqual)
        {
            var json = JsonConvert.SerializeObject(Configuration, Formatting.Indented);
            _fileSystem.File.WriteAllText(_configurationFilePath, json);
            if (notify)
            {
                var changedProperties = result.Differences.Select(x => x.PropertyName).ToArray();
                ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(changedProperties));
            }
            _loadedConfiguration = new ContainerDesktopConfiguration(_productInformation);
            JsonConvert.PopulateObject(json, _loadedConfiguration);
        }
    }

    protected void Load(bool notify)
    {
        var json = _fileSystem.File.ReadAllText(_configurationFilePath);
        var loaded = new ContainerDesktopConfiguration(_productInformation);
        JsonConvert.PopulateObject(json, loaded);
        var result = _comparer.Compare(Configuration, loaded);
        if(!result.AreEqual)
        {
            JsonConvert.PopulateObject(json, Configuration);
            if (notify)
            {
                var changedProperties = result.Differences.Select(x => x.PropertyName).ToArray();
                ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(changedProperties));
            }
        }
        _loadedConfiguration = loaded;
    }
}
