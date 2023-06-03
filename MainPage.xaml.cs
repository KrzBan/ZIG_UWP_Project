using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.UI.Xaml.Controls;

namespace ZIG_UWP_Project
{

    public class Item : INotifyPropertyChanged
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if(_name != value)
                {
                    _name = value;
                    RaisePropertyChanged("Name");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<Item> Children { get; set; } = new ObservableCollection<Item>();

        public override string ToString()
        {
            return Name;
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed partial class MainPage : Page
    {
        public ObservableCollection<Item> Scene { get; set; } = new ObservableCollection<Item>();
        
        public MainPage()
        {
            this.InitializeComponent();
            Scene = GetDefaultScene();
        }

        private Item ItemFromXElement(XElement element)
        {
            var item = new Item();
            item.Name = element.Element("Name").Value;

            foreach(var chid in element.Element("Children").Elements("Item"))
            {
                item.Children.Add(ItemFromXElement(chid));
            }

            return item;
        }
        private void LoadSceneFromXDocument(XDocument document)
        {
            var list = new ObservableCollection<Item>();

            var items = document.Element("Scene").Elements("Item");
            foreach(var item in items)
            {
                list.Add(ItemFromXElement(item));
            }

            Scene.Clear();
            foreach(var item in list)
            {
                Scene.Add(item);
            }            
        }
        public async Task LoadSceneFromFile()
        {
            Debug.WriteLine("Loading scene..");

            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.FileTypeFilter.Add(".xml");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                Debug.WriteLine("Picked File: " + file.Name);
                var read = await file.OpenStreamForReadAsync();
                
                var document = XDocument.Load(read);

                LoadSceneFromXDocument(document);
            }
            else
            {
                Debug.WriteLine("Operation cancelled.");
            }
        }

        private XElement[] ItemsToXElements(List<Item> items)
        {
            var list = new List<XElement> ();

            foreach(var item in items)
            {
                var elem = new XElement("Item", 
                    new XElement("Name", item.Name), 
                    new XElement("Children", ItemsToXElements(item.Children.ToList())));
                list.Add(elem);
            }

            return list.ToArray();
        }
        public async Task SaveSceneToFile()
        {
            Debug.WriteLine("Saving scene..");

            XDocument document = new XDocument(
                new XElement("Scene", ItemsToXElements(Scene.ToList()))
            );
            
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.FileTypeChoices.Add("Scene File", new List<string>() { ".xml" });
            savePicker.SuggestedFileName = "Scene";

            Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                Windows.Storage.CachedFileManager.DeferUpdates(file);
                
                await Windows.Storage.FileIO.WriteTextAsync(file, document.ToString());
                
                Windows.Storage.Provider.FileUpdateStatus status =
                    await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
                if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
                {
                    Debug.WriteLine("File " + file.Name + " was saved.");
                }
                else
                {
                    Debug.WriteLine("File " + file.Name + " couldn't be saved.");
                }
            }
            else
            {
                Debug.WriteLine("Operation cancelled.");
            }
        }

        private ObservableCollection<Item> GetDefaultScene()
        {
            var list = new ObservableCollection<Item>();

            Item root = new Item()
            {
                Name = "Root",
                Children =
                {
                    new Item(){Name = "Default 1"},
                    new Item(){Name = "Default 2"},
                    new Item(){Name = "Default 3"}
                }
            };

            list.Add(root);

            return list;
        }

        private void Button_AddItem()
        {
            var parentCollection = Scene;
            var selected = SceneTree.SelectedItem as Item;
            
            if(selected != null)
            {
                parentCollection = selected.Children;
            }

            parentCollection.Add(new Item() { Name = "New" });
        }

        private void Button_RemoveSelectedItem()
        {
            var parentList = Scene;
            var selectedNode = SceneTree.SelectedNode;

            if (selectedNode != null)
            {
                var parentNode = selectedNode.Parent;
                var parentItem = parentNode.Content as Item;
                if (parentItem != null)
                {
                    parentList = parentItem.Children;
                }

                parentList.Remove(SceneTree.SelectedItem as Item);
            }
        }
    }
}
