using System.Windows;

// WPF looks for theme specific resource dictionaries in satellite assemblies and for the
// generic (theme independent) ones inside this assembly. This app ships neither kind, so
// None tells WPF not to search for the theme specific dictionaries at all.
[assembly: ThemeInfo(
  ResourceDictionaryLocation.None,
  ResourceDictionaryLocation.SourceAssembly
)]
