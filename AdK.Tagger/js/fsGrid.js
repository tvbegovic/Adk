var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var fsGridModule;
(function (fsGridModule) {
    var Filter = (function () {
        function Filter(emptyValueIds) {
            this.emptyIds = emptyValueIds || FilterByChoice.defaultEmptyIds;
        }
        Filter.prototype.isEmpty = function (id) {
            return this.emptyIds.indexOf(id) !== -1;
        };
        Filter.prototype.isActive = function () {
            return true;
        };
        Filter.prototype.matches = function (itemId) {
            return true;
        };
        Filter.defaultEmptyIds = [undefined, null, '', 0];
        return Filter;
    }());
    var FilterByChoice = (function (_super) {
        __extends(FilterByChoice, _super);
        function FilterByChoice(emptyValueIds) {
            _super.call(this, emptyValueIds);
        }
        FilterByChoice.prototype.set = function (choice) {
            this.filteredBy = choice;
        };
        FilterByChoice.prototype.setChoices = function (choices) {
            this.choices = choices;
            this.handleEmptyValue();
            this.addAllValuesOption();
        };
        FilterByChoice.prototype.isActive = function () {
            return this.filteredBy && this.filteredBy.id !== FilterByChoice.AllValues;
        };
        FilterByChoice.prototype.handleEmptyValue = function () {
            var _this = this;
            this.hasEmptyValue = false;
            var emptyValueIndex = _.findIndex(this.choices, function (choice) { return _this.isEmpty(choice.id); }, this);
            if (emptyValueIndex !== -1) {
                this.choices.splice(emptyValueIndex, 1);
                this.hasEmptyValue = true;
                this.choices.splice(0, 0, { id: FilterByChoice.EmptyValue, label: '(empty)' });
            }
        };
        FilterByChoice.prototype.addAllValuesOption = function () {
            var allValuesOption = { id: FilterByChoice.AllValues, label: '(all)' };
            this.choices.splice(0, 0, allValuesOption);
            this.filteredBy = allValuesOption;
        };
        FilterByChoice.prototype.matches = function (itemId) {
            var match = !this.filteredBy ||
                this.filteredBy.id === FilterByChoice.AllValues ||
                (this.filteredBy.id === FilterByChoice.EmptyValue && this.isEmpty(itemId)) ||
                this.filteredBy.id === itemId;
            return match;
        };
        FilterByChoice.AllValues = '__fs_grid_all_values__';
        FilterByChoice.EmptyValue = '__fs_grid_empty_value__';
        return FilterByChoice;
    }(Filter));
    var FilterChoiceList = (function (_super) {
        __extends(FilterChoiceList, _super);
        function FilterChoiceList(choices, emptyValueIds) {
            _super.call(this, emptyValueIds);
            this.setChoices(choices);
        }
        return FilterChoiceList;
    }(FilterByChoice));
    var FilterSubstring = (function (_super) {
        __extends(FilterSubstring, _super);
        function FilterSubstring() {
            _super.apply(this, arguments);
            this.substring = '';
            this.editing = false;
        }
        FilterSubstring.prototype.isActive = function () {
            return this.substring !== '';
        };
        FilterSubstring.prototype.matches = function (itemId) {
            if (this.substring === '')
                return true;
            if (this.isEmpty(itemId))
                return false;
            var sItemId = String(itemId);
            return sItemId.toLowerCase().indexOf(this.substring.toLowerCase()) !== -1;
        };
        return FilterSubstring;
    }(Filter));
    fsGridModule.FilterSubstring = FilterSubstring;
    var FilterFromData = (function (_super) {
        __extends(FilterFromData, _super);
        function FilterFromData(data, columnName, emptyValueIds) {
            _super.call(this, emptyValueIds);
            this.data = data;
            this.columnName = columnName;
            this.setChoices(this.getUniqueValues());
        }
        FilterFromData.prototype.getUniqueValues = function () {
            var values = _.uniq(_.pluck(this.data, this.columnName)).map(function (value) {
                return { id: value, label: value };
            });
            return values;
        };
        return FilterFromData;
    }(FilterByChoice));
    fsGridModule.FilterFromData = FilterFromData;
    var Column = (function () {
        function Column(grid, header, attribute, filter) {
            this.grid = grid;
            this.header = header;
            this.attribute = attribute;
            this.filter = filter;
            grid.addColumn(this);
        }
        Column.prototype.isFilterActive = function () {
            return this.filter && this.filter.isActive();
        };
        Column.prototype.filterItem = function (item) {
            return !this.filter || this.filter.matches(item[this.attribute]);
        };
        Column.prototype.displayFilter = function (fn) {
            this._displayFilter = fn;
        };
        Column.prototype.display = function (item) {
            if (!this._displayFilter)
                return item[this.attribute];
            return this._displayFilter(item[this.attribute]);
        };
        return Column;
    }());
    fsGridModule.Column = Column;
    var Grid = (function () {
        function Grid(data) {
            this.data = data;
            this.columns = [];
            this.sort = { column: null, ascending: true };
        }
        Grid.prototype.addColumn = function (column) {
            this.columns.push(column);
            if (!this.sort.column)
                this.sort.column = column;
        };
        Grid.prototype.filterItem = function (item) {
            return _.every(this.columns, function (column) { return column.filterItem(item); });
        };
        Grid.prototype.orderKey = function (item) {
            return this.sort.column ? item[this.sort.column.attribute] : null;
        };
        Grid.prototype.toggleSorting = function (column) {
            if (this.sort.column === column)
                this.sort.ascending = !this.sort.ascending;
            else {
                this.sort.column = column;
                this.sort.ascending = true; // or defaultSorting(column)
            }
        };
        return Grid;
    }());
    fsGridModule.Grid = Grid;
})(fsGridModule || (fsGridModule = {}));
//# sourceMappingURL=fsGrid.js.map