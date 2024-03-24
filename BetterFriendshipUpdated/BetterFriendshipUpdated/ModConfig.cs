﻿using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BetterFriendshipUpdated
{
    internal class ModConfig : INotifyPropertyChanged
    {
        public bool DisplayTalkPrompts { get; set; } = true;
        public bool DisplayGenericGiftPrompts { get; set; } = true;
        public int GiftCycleDelay { get; set; } = 2000;
        public bool IgnoreMaxedFriendships { get; set; }
        public bool SpousePromptsOverride { get; set; }
        public bool DisplayBubbles { get; set; } = true;

        private int _bubbleDisplayRange = 3; // nice for only up-close bubbles
        
        public int BubbleDisplayRange
        {
            get => _bubbleDisplayRange;
            set
            {
                _bubbleDisplayRange = value;
                OnPropertyChanged();
            }
        }

        private string _giftPreference = "like";
        private int _giftCycleCount = 5;
        private bool _onlyHighestQuality;

        public string GiftPreference
        {
            get => _giftPreference;
            set
            {
                _giftPreference = value;
                OnPropertyChanged();
            }
        }

        public int GiftCycleCount
        {
            get => _giftCycleCount;
            set
            {
                _giftCycleCount = value;
                OnPropertyChanged();
            }
        }

        public bool OnlyHighestQuality
        {
            get => _onlyHighestQuality;
            set
            {
                _onlyHighestQuality = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}