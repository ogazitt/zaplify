using System;
#if CLIENT
using BuiltSteady.Zaplify.Devices.ClientEntities;
#else
using BuiltSteady.Zaplify.ServerEntities;
#endif

namespace BuiltSteady.Zaplify.Shared.Entities
{

    public class ItemAccessor
    {
        Item item;

        public ItemAccessor(Item item)
        {
            this.item = item;
        }

        public string Get(string fieldName)
        {
            FieldValue fv = item.GetFieldValue(fieldName);
            return (fv == null) ? null : fv.Value;
        }
        public void Set(string fieldName, string value)
        {
            FieldValue fv = item.GetFieldValue(fieldName, true);
            fv.Value = value;
        }

        public bool GetBool(string fieldName)
        {
            bool? value = GetNullableBool(fieldName);
            return (value == null) ? false : (bool)value;
        }
        public bool? GetNullableBool(string fieldName)
        {
            FieldValue fv = item.GetFieldValue(fieldName);
            if (fv != null) { return fv.Value.ToLower().Equals("true"); }
            return null;
        }
        public void SetBool(string fieldName, bool value)
        {
            FieldValue fv = item.GetFieldValue(fieldName, true);
            fv.Value = value.ToString();
        }

        public int GetInt(string fieldName, int defaultValue = 0)
        {
            int? value = GetNullableInt(fieldName);
            return (value == null) ? defaultValue : (int)value;
        }
        public int? GetNullableInt(string fieldName)
        {
            int value;
            FieldValue fv = item.GetFieldValue(fieldName);
            if (fv != null && int.TryParse(fv.Value, out value))
            {
                return value;
            }
            return null;
        }
        public void SetInt(string fieldName, int value)
        {
            FieldValue fv = item.GetFieldValue(fieldName, true);
            fv.Value = value.ToString();
        }

        public DateTime GetDate(string fieldName, DateTime? defaultValue = null)
        {
            if (defaultValue == null) { defaultValue = DateTime.Now; }
            DateTime? value = GetNullableDate(fieldName);
            return (value == null) ? (DateTime)defaultValue : (DateTime)value;
        }
        public DateTime? GetNullableDate(string fieldName)
        {
            FieldValue fv = item.GetFieldValue(fieldName);
            if (fv != null) { return Convert.ToDateTime(fv.Value); }
            return null;
        }
        public void SetDate(string fieldName, DateTime value)
        {   // store dates as UTC in RFC3339-compatible format
            FieldValue fv = item.GetFieldValue(fieldName, true);
            fv.Value = value.ToUniversalTime().ToString("o");
        }

    }
}