//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.ComponentModel;

namespace Seal.Converter
{
    public class WidgetIconClassConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true; //true means show a combobox
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false; //true will limit to list. false will show the list, but allow free-form entry
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(
                new string[] {
                    "glyphicon glyphicon-info-sign",
                    "glyphicon glyphicon-warning-sign",
                    "glyphicon glyphicon-ok",
                    "glyphicon glyphicon-remove",
                    "glyphicon glyphicon-envelope",
                    "glyphicon glyphicon-search",
                    "glyphicon glyphicon-star",
                    "glyphicon glyphicon-signal",
                    "glyphicon glyphicon-cog",
                    "glyphicon glyphicon-list-alt",
                    "glyphicon glyphicon-education",
                    "glyphicon glyphicon-thumbs-up",
                    "glyphicon glyphicon-bell",
                    "glyphicon glyphicon-folder-open",
                    "glyphicon glyphicon-pencil",
                    "glyphicon glyphicon-usd",
                    "glyphicon glyphicon-user",
                    "glyphicon glyphicon-euro"
                });
        }
    }

}
