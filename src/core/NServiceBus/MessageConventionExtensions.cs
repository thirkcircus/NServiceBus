﻿namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using log4net;

    /// <summary>
    /// Extension methods for message related conventions
    /// </summary>
    public static class MessageConventionExtensions
    {
        static readonly ILog Logger = LogManager.GetLogger("NServiceBus");
        /// <summary>
        /// Returns true if the given object is a message.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static bool IsMessage(this object o)
        {
            return o.GetType().IsMessageType();
        }

        /// <summary>
        /// Returns true if the given type is a message type.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsMessageType(this Type t)
        {
            try
            {
                return MessagesConventionCache.ApplyConvention(t,
                                                               type =>
                                                               IsMessageTypeAction(type) || IsCommandTypeAction(type) ||
                                                               IsEventTypeAction(type) || (IsInSystemConventionList(type)));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to evaluate Message convention: " + ex);           
                throw;
            }
        }

        private static bool IsInSystemConventionList(this Type t)
        {
            return IsSystemMessageActions.Any(isSystemMessageAction => isSystemMessageAction(t));
        }

        /// <summary>
        /// Add system message convention
        /// </summary>
        /// <param name="definesMessageType">Function to define system message convention</param>
        public static void AddSystemMessagesConventions(Func<Type, bool> definesMessageType)
        {
            if(!IsSystemMessageActions.Contains(definesMessageType))
                IsSystemMessageActions.Add(definesMessageType);
        }

        /// <summary>
        /// Returns true if the given object is a command.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static bool IsCommand(this object o)
        {
                return o.GetType().IsCommandType();
        }

        /// <summary>
        /// Returns true if the given type is a command type.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsCommandType(this Type t)
        {
            try
            {
                return CommandsConventionCache.ApplyConvention(t, type => IsCommandTypeAction(type));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to evaluate Command convention: " + ex);
                throw;
            }
        }

        /// <summary>
        /// Returns true if the given message should not be written to disk when sent.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static bool IsExpressMessage(object o)
        {
            return IsExpressMessageType(o.GetType());
        }

        /// <summary>
        /// Returns true if the given type should not be written to disk when sent.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsExpressMessageType(Type t)
        {
            try
            {
                return ExpressConventionCache.ApplyConvention(t, type => IsExpressMessageAction(type));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to evaluate Express convention: " + ex);
                throw;
            }
        }

        /// <summary>
        /// Returns true if the given property should be encrypted
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public static bool IsEncryptedProperty(this PropertyInfo property)
        {
            try
            {
                //the message mutator will cache the whole message so we don't need to cache here
                return IsEncryptedPropertyAction(property);

            }
            catch (Exception ex)
            {
                Logger.Error("Failed to evaluate Encrypted Property convention: " + ex);
                throw;
            }
        }

        /// <summary>
        /// Returns true if the given property should be send via the DataBus
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public static bool IsDataBusProperty(this PropertyInfo property)
        {
            try
            {
                return IsDataBusPropertyAction(property);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to evaluate DataBus Property convention: " + ex);
                throw;
            }
        }

        /// <summary>
        /// Returns true if the given object is a event.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static bool IsEvent(this object o)
        {
            return o.GetType().IsEventType();
        }

        /// <summary>
        /// Returns true if the given type is a event type.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsEventType(this Type t)
        {
            try
            {
                return EventsConventionCache.ApplyConvention(t, type => IsEventTypeAction(type));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to evaluate Event convention: " + ex);
                throw;
            }
        }

        /// <summary>
        /// The function used to determine whether a type is a message type.
        /// </summary>
        public static Func<Type, bool> IsMessageTypeAction = t => typeof(IMessage).IsAssignableFrom(t) &&
                                                                    typeof(IMessage) != t &&
                                                                    typeof(IEvent) != t &&
                                                                    typeof(ICommand) != t;

        /// <summary>
        /// The function used to determine whether a type is a command type.
        /// </summary>
        public static Func<Type, bool> IsCommandTypeAction = t => typeof(ICommand).IsAssignableFrom(t) && typeof(ICommand) != t;

        /// <summary>
        /// The function used to determine whether a type is a event type.
        /// </summary>
        public static Func<Type, bool> IsEventTypeAction = t => typeof(IEvent).IsAssignableFrom(t) && typeof(IEvent) != t;

        /// <summary>
        /// The function used to determine whether a property should be encrypted
        /// </summary>
        public static Func<PropertyInfo, bool> IsEncryptedPropertyAction = property => typeof(WireEncryptedString).IsAssignableFrom(property.PropertyType);

        /// <summary>
        /// The function used to determine whether a property should be treated as a databus property.
        /// </summary>
        public static Func<PropertyInfo, bool> IsDataBusPropertyAction = property => typeof(IDataBusProperty).IsAssignableFrom(property.PropertyType) && typeof(IDataBusProperty) != property.PropertyType;

        /// <summary>
        /// The function to evaluate wheather the message has a time to be received or not (<value>TimeSpan.MaxValue</value>).
        /// </summary>
        public static Func<Type, TimeSpan> TimeToBeReceivedAction = t =>
                                                                                  {
                                                                                      var attributes = t.GetCustomAttributes(typeof (TimeToBeReceivedAttribute), true)
                                                                                          .Select(s => s as TimeToBeReceivedAttribute)
                                                                                          .ToList();

                                                                                      return attributes.Count > 0 ? attributes.Last().TimeToBeReceived : TimeSpan.MaxValue;
                                                                                  };

        /// <summary>
        /// The function used to determine if a type is an express message (the message should not be written to disk).
        /// </summary>
        public static Func<Type, bool> IsExpressMessageAction = t => t.GetCustomAttributes(typeof(ExpressAttribute), true)
                                                                  .Any();

       /// <summary>
       /// Contains list of System messages' conventions
       /// </summary>
        public static List<Func<Type, bool>> IsSystemMessageActions = new List<Func<Type, bool>>();

        static readonly ConventionCache<Type> MessagesConventionCache = new ConventionCache<Type>();
        static readonly ConventionCache<Type> CommandsConventionCache = new ConventionCache<Type>();
        static readonly ConventionCache<Type> EventsConventionCache = new ConventionCache<Type>();
        static readonly ConventionCache<Type> ExpressConventionCache = new ConventionCache<Type>();
    }

    internal class ConventionCache<T>
    {
        private readonly IDictionary<T, bool> cache = new ConcurrentDictionary<T, bool>();

        public bool ApplyConvention(T type, Func<T, bool> action)
        {
            bool result;

            if (cache.TryGetValue(type, out result))
                return result;

            result = action(type);

            cache[type] = result;

            return result;
        }
    }
}
