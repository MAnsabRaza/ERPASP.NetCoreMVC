﻿namespace ERP.Models.ChartOfItem
{
    public class Category
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }
        public string category_name { get; set; }
        public string? category_description { get; set; }
        public bool status { get; set; }

    }
}
