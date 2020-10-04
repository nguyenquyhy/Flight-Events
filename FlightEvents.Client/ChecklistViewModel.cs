using FlightEvents.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FlightEvents.Client
{
    public class ChecklistViewModel : BaseViewModel
    {
        public ChecklistViewModel(FlightEvent flightEvent, bool connectedToDiscord)
        {
            Name = flightEvent.Name;
            StartDateTime = flightEvent.StartDateTime;
            EndDateTime = flightEvent.EndDateTime;

            if (flightEvent.ChecklistItems != null)
            {
                foreach (var item in flightEvent.ChecklistItems)
                {
                    var itemVM = new ChecklistItemViewModel(item)
                    {
                        Title = item.Title,
                        Links = item.Links
                    };
                    switch (item.Type)
                    {
                        case FlightEventChecklistItemType.Client:
                            switch (item.SubType)
                            {
                                case FlightEventChecklistItemSubType.ConnectToDiscord:
                                    itemVM.IsChecked = connectedToDiscord;
                                    itemVM.IsEnabled = false;
                                    itemVM.Hint = "To connect Flight Events to Discord:\n- Open Discord tab\n- Click Connect\n- Login to your Discord account\n- Authorize Flight Events bot to get your Discord information\n- Copy the Code back to this client\n- Press Confirm";
                                    break;
                            }
                            Items.Add(itemVM);
                            break;
                        default:
                            Items.Add(itemVM);
                            break;
                    }
                }
            }
        }

        private string name;
        public string Name { get => name; set => SetProperty(ref name, value); }

        private DateTimeOffset startDateTime;
        public DateTimeOffset StartDateTime { get => startDateTime; set => SetProperty(ref startDateTime, value); }

        private DateTimeOffset? endDateTime;
        public DateTimeOffset? EndDateTime { get => endDateTime; set => SetProperty(ref endDateTime, value); }

        private ObservableCollection<ChecklistItemViewModel> items = new ObservableCollection<ChecklistItemViewModel>();
        public ObservableCollection<ChecklistItemViewModel> Items { get => items; set => SetProperty(ref items, value); }
    }

    public class ChecklistItemViewModel : BaseViewModel
    {
        public ChecklistItemViewModel(FlightEventChecklistItem data)
        {
            Data = data;
        }

        public FlightEventChecklistItem Data { get; }

        private bool isChecked;
        public bool IsChecked { get => isChecked; set { if (IsEnabled) SetProperty(ref isChecked, value); } }

        public bool IsEnabled { get; set; } = true;

        public string Title { get; set; }
        public string Hint { get; set; }
        public List<FlightEventChecklistItemLink> Links { get; set; }
    }
}
