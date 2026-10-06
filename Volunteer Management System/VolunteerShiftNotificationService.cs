// Purpose:
// Stores assignment notifications for Feature 5 and lets volunteers read
// notifications belonging to their own account.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Volunteer_Management_System
{
    // Provides the prototype's in-app shift notification operations.
    // Notifications use in-memory storage, matching the current assignment
    // service, and therefore reset when the application restarts.
    public class VolunteerShiftNotificationService
    {
        private readonly List<VolunteerShiftNotification> _notifications = new();
        private readonly object _syncRoot = new();

        // Raised after notifications change so open Blazor components can
        // refresh their unread count without polling.
        public event Action? NotificationsChanged;

        // Records a notification when a volunteer receives a shift.
        public VolunteerShiftNotification NotifyAssigned(
            VolunteerAssignment assignment,
            VolunteerOpportunity opportunity)
        {
            return AddNotification(
                assignment,
                opportunity,
                ShiftNotificationType.Assigned);
        }

        // Records a notification when an assigned shift is cancelled.
        public VolunteerShiftNotification NotifyCancelled(
            VolunteerAssignment assignment,
            VolunteerOpportunity opportunity)
        {
            return AddNotification(
                assignment,
                opportunity,
                ShiftNotificationType.Cancelled);
        }

        // Returns the volunteer's notifications, newest first.
        public IReadOnlyList<VolunteerShiftNotification>
            GetNotificationsForVolunteer(Guid volunteerId)
        {
            ValidateVolunteerId(volunteerId);

            lock (_syncRoot)
            {
                return _notifications
                    .Where(notification =>
                        notification.VolunteerId == volunteerId)
                    .OrderByDescending(notification =>
                        notification.CreatedAt)
                    .ToList()
                    .AsReadOnly();
            }
        }

        // Returns only notifications the volunteer has not acknowledged.
        public IReadOnlyList<VolunteerShiftNotification>
            GetUnreadNotificationsForVolunteer(Guid volunteerId)
        {
            return GetNotificationsForVolunteer(volunteerId)
                .Where(notification => !notification.IsRead)
                .ToList()
                .AsReadOnly();
        }

        // Marks all notifications belonging to one volunteer as read.
        public void MarkAllAsRead(Guid volunteerId)
        {
            ValidateVolunteerId(volunteerId);
            bool changed = false;

            lock (_syncRoot)
            {
                foreach (VolunteerShiftNotification notification
                    in _notifications.Where(notification =>
                        notification.VolunteerId == volunteerId &&
                        !notification.IsRead))
                {
                    notification.MarkAsRead();
                    changed = true;
                }
            }

            if (changed)
            {
                NotificationsChanged?.Invoke();
            }
        }

        private VolunteerShiftNotification AddNotification(
            VolunteerAssignment assignment,
            VolunteerOpportunity opportunity,
            ShiftNotificationType type)
        {
            VolunteerShiftNotification notification =
                VolunteerShiftNotification.Create(
                    assignment,
                    opportunity,
                    type);

            lock (_syncRoot)
            {
                _notifications.Add(notification);
            }

            NotificationsChanged?.Invoke();
            return notification;
        }

        private static void ValidateVolunteerId(Guid volunteerId)
        {
            if (volunteerId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Volunteer id is required.",
                    nameof(volunteerId));
            }
        }
    }
}
