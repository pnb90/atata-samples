﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Atata;
using OpenQA.Selenium;

namespace AtataSamples.TableWithRowSpannedCells
{
    public class FindByColumnHeaderInTableWithRowSpannedCellsStrategy : IComponentScopeFindStrategy
    {
        protected static ConcurrentDictionary<Type, List<ColumnInfo>> TableColumnsInfoCache { get; } =
            new ConcurrentDictionary<Type, List<ColumnInfo>>();

        public string RowXPath { get; set; } = "tr";

        public string HeaderCellsXPath { get; set; } = "(ancestor::table)[position() = last()]/thead//th";

        public string RowWithSpannedCellsXPathCondition { get; set; } = "td[@rowspan and normalize-space(@rowspan) != '1']";

        public ComponentScopeLocateResult Find(ISearchContext scope, ComponentScopeLocateOptions options, SearchOptions searchOptions)
        {
            string xPath = BuildXPath(scope, options);

            if (xPath == null)
            {
                if (searchOptions.IsSafely)
                    return new MissingComponentScopeFindResult();
                else
                    throw ExceptionFactory.CreateForNoSuchElement(options.GetTermsAsString(), searchContext: scope);
            }

            ComponentScopeLocateOptions xPathOptions = options.Clone();
            xPathOptions.Index = 0;
            xPathOptions.Terms = new string[] { xPath };

            return new SubsequentComponentScopeFindResult(scope, new FindByXPathStrategy(), xPathOptions);
        }

        protected virtual string BuildXPath(ISearchContext scope, ComponentScopeLocateOptions options)
        {
            List<ColumnInfo> columns = TableColumnsInfoCache.GetOrAdd(
                options.Metadata.ParentComponentType,
                _ => GetColumnInfoItems((IWebElement)scope));

            ColumnInfo column = columns.
                Where(x => options.Match.IsMatch(x.HeaderName, options.Terms)).
                ElementAtOrDefault(options.Index ?? 0);

            return column != null ? BuildXPathForCell(column, columns) : null;
        }

        protected virtual string BuildXPathForCell(ColumnInfo column, List<ColumnInfo> columns)
        {
            string rowSpannedCellXPathCondition = $"count(td) = {columns.Count}";
            int columnIndex = columns.IndexOf(column);

            if (column.HasRowSpan)
            {
                return $"(self::{RowXPath} | preceding-sibling::{RowXPath})[{rowSpannedCellXPathCondition}][last()]/td[{columnIndex + 1}]";
            }
            else
            {
                int countOfPrecedingColumnsWithoutRowSpan = columns.Take(columnIndex).Count(x => !x.HasRowSpan);
                return $"(self::{RowXPath}[{rowSpannedCellXPathCondition}]/td[{columnIndex + 1}] | self::{RowXPath}[not({rowSpannedCellXPathCondition})]/td[{countOfPrecedingColumnsWithoutRowSpan + 1}])";
            }
        }

        protected virtual List<ColumnInfo> GetColumnInfoItems(IWebElement row)
        {
            var headers = GetHeaderCells(row);
            var cells = GetCellsOfRowWithSpannedCells(row);

            return headers.Select((header, index) =>
            {
                string cellRowSpanValue = cells.ElementAtOrDefault(index)?.GetAttribute("rowspan")?.Trim();

                return new ColumnInfo
                {
                    HeaderName = header.Text,
                    HasRowSpan = !string.IsNullOrEmpty(cellRowSpanValue) && cellRowSpanValue != "1"
                };
            }).ToList();
        }

        private ReadOnlyCollection<IWebElement> GetHeaderCells(IWebElement row)
        {
            return row.GetAll(By.XPath(HeaderCellsXPath).AtOnce().OfAnyVisibility());
        }

        private ReadOnlyCollection<IWebElement> GetCellsOfRowWithSpannedCells(IWebElement row)
        {
            ReadOnlyCollection<IWebElement> cells = row.GetAll(
                By.XPath($"../{RowXPath}[{RowWithSpannedCellsXPathCondition}][1]/td").AtOnce().OfAnyVisibility());

            return cells.Any()
                ? cells
                : row.GetAll(By.XPath("./td").AtOnce().OfAnyVisibility());
        }

        protected class ColumnInfo
        {
            public string HeaderName { get; set; }

            public bool HasRowSpan { get; set; }
        }
    }
}
