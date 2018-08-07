﻿using System;

namespace Toggl.Daneel.Views.Calendar
{
    public struct CalendarCollectionViewItemLayoutAttributes
    {
        public DateTime StartTime { get; }

        public TimeSpan Duration { get; }

        public DateTime EndTime => StartTime + Duration;

        public int OverlappingItemsCount { get; }

        public int PositionInOverlappingGroup { get; }

        public CalendarCollectionViewItemLayoutAttributes(DateTime startTime,
            TimeSpan duration,
            int overlappingItemsCount,
            int positionInOverlappingGroup)
        {
            StartTime = startTime;
            Duration = duration;
            OverlappingItemsCount = overlappingItemsCount;
            PositionInOverlappingGroup = positionInOverlappingGroup;
        }
    }
}
