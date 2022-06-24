using System.Collections.Generic;
using System.Linq;
using AElf.WebApp.MessageQueue.Dtos;
using AElf.WebApp.MessageQueue.Entities;

namespace AElf.WebApp.MessageQueue.Helpers
{
    public static class EventFilterSetHelper
    {
        public static List<EventFilterSet> TransferToEventFilterSet(IEnumerable<EventFilterEntity> eventFilters)
        {
            var eventFilterSets = new List<EventFilterSet>();
            foreach (var eventFilter in eventFilters)
            {
                foreach (var eventDetail in eventFilter.EventDetails)
                {
                    foreach (var eventName in eventDetail.Names)
                    {
                        var targetEventFilterSet =
                            eventFilterSets.FirstOrDefault(x => x.IsEqualToEvent(eventDetail.Address, eventName));
                        if (targetEventFilterSet == null)
                        {
                            eventFilterSets.Add(new EventFilterSet(eventDetail.Address, eventName,
                                eventFilter.Id));
                        }
                        else if(!targetEventFilterSet.FilterIds.Contains(eventFilter.Id))
                        {
                            targetEventFilterSet.FilterIds.Add(eventFilter.Id);
                        }
                    }
                }
            }

            return eventFilterSets;
        }
    }
}