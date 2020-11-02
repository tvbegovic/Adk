angular.module('app')
    .controller('priceDesigner11ChannelCtrl', ['$scope', '$filter', '$modal', '$routeParams', '$timeout', '$location', 'confirmPopup', 'Service', 'priceDesigner11Service', function ($scope, $filter, $modal, $routeParams, $timeout, $location, confirmPopup, Service, priceDesigner11Service) {
        $scope.id = $routeParams.id;

        $scope.data = priceDesigner11Service;

        var shouldLoadChannel = false;

        if (priceDesigner11Service.Id != $scope.id) {
            shouldLoadChannel = true;            
            priceDesigner11Service.init();
            priceDesigner11Service.Id = $scope.id;
        }
		
        /*$scope.channel = priceDesigner11Service.channel;
        $scope.data = priceDesigner11Service.data;
        $scope.selectedDayPart = null; //priceDesigner11Service.selectedDayPart;
        $scope.AllProducts = priceDesigner11Service.AllProducts;
        var dayPartsUsageCount = priceDesigner11Service.dayPartsUsageCount;*/
        $scope.priceModes = [{ Id: 1, Name: 'Per second' }, { Id: 2, Name: 'Exact' }];
        //var removedDayPartsIds = priceDesigner11Service.removedDayPartsIds;
        var tabDown = false;
        //var listPriceDefs = priceDesigner11Service.listPriceDefs;
        $scope.weekActive = true;
        $scope.productsActive = false;
		                            

        function loadChannel() {
            Service('GetChannelWithPriceDefinitions11', { channelId: $scope.id }).then(function (channel) {
                $scope.data.channel = channel;
                //$scope.channel = priceDesigner11Service.channel;
                
                $scope.data.channel.Products.forEach(function (p) {
                    p.Codes = _.filter(channel.ProductCodes, { ProductId: p.Id });
                });
                //$scope.channel.Products.push({ Id: -1, Name: "<Add New>", Codes: [] });
                var matrix = $scope.data.channel.PriceDefsMatrix
                for (var i = 0; i < matrix.length; i++) {
                    for (var j = 0; j < matrix[i].length; j++) {
                        var newId = matrix[i][j].Id;
                        if (newId > 0)
                        {
                            if (newId in $scope.data.dayPartsUsageCount)
                                $scope.data.dayPartsUsageCount[newId].count++;
                            else
                            {
                                $scope.data.dayPartsUsageCount[newId] = { part: matrix[i][j], count: 1 };
                            }
                                
                        }
                    }
                }

                Service('GetProducts').then(function (products) {
                    $scope.data.AllProducts = products;
                    //$scope.AllProducts = priceDesigner11Service.AllProducts;
                });

                if ($scope.data.channel.Products.length > 0)
                    $scope.data.channel.selectedProduct = $scope.data.channel.Products[0];
                else
                    $scope.data.channel.selectedProduct = null;

                
            });
        };

        /*$scope.$watch('channel.selectedProduct', function () {
			if($scope.channel != null && $scope.channel.selectedProduct != null)
			  $scope.onSelectedProductChange();
        });*/

        $scope.addNewProduct = function()
        {
            openProductsModal();
        }

        $scope.dayPartCellClicked = function (dayPart, x, y, $event) {

            deselectAllDayPartCells();

            if ($event.shiftKey) {//If shift+click then select rectangle

                var yCounter = y;

                while (true) {

                    var xCounter = x;

                    while (true) {

                        $scope.data.channel.PriceDefsMatrix[yCounter][xCounter].Selected = true;

                        if (xCounter == $scope.data.firstCell.x)
                            break;

                        if (xCounter > $scope.data.firstCell.x) {
                            xCounter--;
                        }
                        else if (xCounter < $scope.data.firstCell.x) {
                            xCounter++;
                        }

                    }

                    if (yCounter == $scope.data.firstCell.y)
                        break;

                    if (yCounter > $scope.data.firstCell.y) {
                        yCounter--;
                    }
                    else if (yCounter < $scope.data.firstCell.y) {
                        yCounter++;
                    }

                }

            }
            else {

                $scope.data.firstCell = { x: x, y: y };

            }
                        
            dayPart.Selected = true;

        }

        function deselectAllDayPartCells() {

            var matrix = $scope.data.channel.PriceDefsMatrix;

            for (var i = 0; i < matrix.length; i++) {
                for (var j = 0; j < matrix[i].length; j++) {
                    if(matrix[i][j] != null)
                        matrix[i][j].Selected = false;
                }
            }
        }

        $scope.dayPartRowClicked = function (d) {
            $scope.data.channel.DayParts.forEach(function (dp) {
                dp.Selected = false;
            });
            d.Selected = true;
            $scope.data.selectedDayPart = d;
        };

        $scope.assign = function () {
            $scope.data.pendingSave = true;

            var matrix = $scope.data.channel.PriceDefsMatrix;

            for (var i = 0; i < matrix.length; i++) {
                for (var j = 0; j < matrix[i].length; j++) {
                    if (matrix[i][j].Selected) {
                        var dayPart = matrix[i][j];
                        if (dayPart.Id > 0)
                        {
                            $scope.data.dayPartsUsageCount[dayPart.Id].count--;
                                                     
                        }
                            
                        matrix[i][j] = JSON.parse(JSON.stringify($scope.data.selectedDayPart));  //Make a clone
                        var newId = $scope.data.selectedDayPart.Id;
                        if (newId in $scope.data.dayPartsUsageCount)
                            $scope.data.dayPartsUsageCount[newId].count++;
                        else
                        {
                            $scope.data.dayPartsUsageCount[newId] = { part: matrix[i][j], count: 1 };
                            //add to product codes
                            addPartToProductCodes($scope.data.selectedDayPart);
                        }
                            
                        matrix[i][j].Selected = true;
                        matrix[i][j].Touched = true;

                    }
                }
            }
        };

        $scope.enableAddHourButton = function () {
            return $scope.data.selectedDayPart != null && $scope.data.firstCell != null;
        };

        $scope.addPart = function () {
            openDayPartModal(null);
        };

        $scope.editPart = function (dp) {
            openDayPartModal(dp);
        }

        function openDayPartModal(dp)
        {
            var modalInstance = $modal.open({
                animation: false,
                templateUrl: 'addNewDayPart.html',
                controller: ['$scope', '$modalInstance', 'saveDayPart', function ($scope, $modalInstance, saveDayPart) {

                    $scope.Short_code = dp != null ? dp.Short_code : '';
                    $scope.Name = dp != null ? dp.Name : '';
                    $scope.Id = dp != null ? dp.Id : 0;

                    $scope.cancel = function () {
                        $modalInstance.close();
                    }

                    $scope.save = function () {
                        saveDayPart($scope.Id, $scope.Short_code, $scope.Name);
                        $modalInstance.close();
                    }
                }],
                resolve: {
                    saveDayPart: function () {
                        return saveDayPart;
                    }
                }
            });
        }

        function saveDayPart(id,short_code, name) {
            var sets = $scope.data.channel.DayPartSets;
            if (sets == null || sets.length == 0)
                Service('CreateDayPartSet', { name: 'My Day parts' }).then(function (set) {
                    sets.push(set);
                    savePart(set.Id, id,short_code, name);
                });
            else
                savePart(sets[0].Id, id,short_code, name);
        }

        function savePart(setId, id,short_code, name) {
            Service(id == 0 ? 'CreateDayPart' : 'UpdateDayPart', { setId: setId, dayPart: { day_part_set_id: setId, short_code: short_code, name: name, id: id } }).then(function (part) {
                if (id == 0)
                {
                    $scope.data.channel.DayParts.push(part);
                    addPartToProductCodes(part);
                }
                else
                {
                    var dp = _.find($scope.data.channel.DayParts, { Id: id });
                    if (dp != null)
                    {
                        dp.Short_code = short_code;
                        dp.Name = name;
                    }
                    updateMatrixDayParts(dp);

                }
            });
        }

        function addPartToProductCodes(part)
        {
            $scope.data.channel.Products.forEach(function (p) {
                p.Codes.forEach(function (c) {
                    c.DayParts.push({ ProductId: $scope.data.channel.selectedProduct.Id, ChannelId: $scope.data.channel.Channel.Id, DayPartId: part.Id, DurationMax: c.DurationMax, PriceMode: c.PriceMode, Price: 0 });
                });
            });
        }

        function updateMatrixDayParts(dp)
        {
            var matrix = $scope.data.channel.PriceDefsMatrix;
            for (var i = 0; i < matrix.length; i++) {
                for (var j = 0; j < matrix[i].length; j++) {
                    if (matrix[i][j].Id == dp.Id)
                    {
                        matrix[i][j].Short_code = dp.Short_code;
                        matrix[i][j].Name = dp.Name;
                    }
                }
            }
        }
		        

        $scope.saveToDatabase = function () {
            confirmPopup.open('Save changes?', null, 'Do you really want to save all changes to the database?').then(function () {

                
                var matrix = $scope.data.channel.PriceDefsMatrix;
                if ($scope.data.channel.PriceDefinitions == null)
                    $scope.data.channel.PriceDefinitions = [];

                for (var j = 0; j < matrix.length; j++) {
                    for (var k = 0; k < matrix[j].length; k++) {
                        if (matrix[j][k].Touched) {

                            var priceDef = _.find($scope.data.channel.PriceDefinitions, { Hour: j, Day: k });
                            if (priceDef == null) {
                                priceDef = {
                                    ChannelId: $scope.id,
                                    DayPartId: matrix[j][k].Id,
                                    Hour: j,
                                    Day: k,
									changed: true
                                };

                                $scope.data.channel.PriceDefinitions.push(priceDef);
                            }
                            else
                            {
                                priceDef.DayPartId = matrix[j][k].Id;
                                priceDef.changed = true;
                            }
                            matrix[j][k].Touched = false;
                            
                        }
                    }
                }
                Service('SaveChannelPriceDefinitions11', { priceDefs: _.filter($scope.data.channel.PriceDefinitions, { changed: true }) }).then(function (updatedDefs) {
                    updatedDefs.forEach(function (upd) {
                        var def = _.find($scope.data.channel.PriceDefinitions, { Hour: upd.Hour, Day: upd.Day });
                        if (def != null)
                        {
                            def.changed = false;
                            def.Id = upd.Id;
                        }
                    });
                    
                    Service('SaveChannelProductDayParts', { productDayParts: CollectProductDayParts(), deleted: $scope.data.removedDayPartsIds }).then(function (productDayParts) {
                        updateNewProductDayParts(productDayParts);
                        $scope.data.removedDayPartsIds = [];
                        $scope.data.pendingSave = false;
                    });
                });


            });
        };

        $scope.productSelected = function (p) {
            $scope.data.channel.selectedProduct = p;
        };



        function openProductsModal()
        {
            var modalInstance = $modal.open({
                animation: false,
                templateUrl: 'addNewProduct.html',
                controller: ['$scope', '$modalInstance','Products','addProduct', 'channel',function ($scope, $modalInstance, Products,addProduct, channel) {

                    
                    $scope.Products = Products;
                    $scope.channel = channel;
                    $scope.Product = Products[0];

                    $scope.cancel = function () {
                        $modalInstance.close();
                    }

                    $scope.save = function () {
                        addProduct($scope.Product);
                        $modalInstance.close();
                    }
                    $scope.productSelected = function(p)
                    {
                        //$scope.Products.forEach(function (p) {
                        //    p.Selected = false;
                        //});
                        //p.Selected = true;
                        $scope.Product = p;
                        $scope.save();
                    };
                }],
                resolve: {
                    Products: function () {
                        return _.filter($scope.data.AllProducts, function (p) {
                            return _.findIndex($scope.data.channel.Products, { Id: p.Id }) < 0;
                        });
                    },
                    addProduct: function () {
                        return addProduct;
                    },
                    channel: function () {
                        return $scope.data.channel.Channel;
                    }
                }
            });
        }

        function addProduct(p)
        {
            $scope.data.channel.Products.splice($scope.data.channel.Products.length - 1, 0, p);
            p.Codes = [];
            $scope.data.channel.selectedProduct = p;
            addProductCode(createNewProductCode(p, '5', 8, 1));
            $timeout(function () {
                document.querySelectorAll('input')[0].focus();
            }, 200);
        }

        function createNewProductCode(product, suffix, duration, priceMode)
        {
            var result = { Id: uuidv4(), Code: product.Name + '-' + suffix, Suffix: suffix, DurationMax: duration, PriceMode: priceMode, ProductId: product.Id, DayParts: [] };
            $scope.selectedDayParts().forEach(function (d) {
                result.DayParts.push({ ProductId: product.Id, ChannelId: $scope.data.channel.Channel.Id, DayPartId: d.Id, DurationMax: duration, PriceMode: priceMode, Price: 0 });
            });
            return result;

        }

        $scope.openProductCodeModal = function()
        {
            var modalInstance = $modal.open({
                animation: false,
                templateUrl: 'addNewCode.html',
                controller: ['$scope', '$modalInstance', 'priceModes', 'addProductCode','product','channel','selectedDayParts',function ($scope, $modalInstance, priceModes, addProductCode,product,channel,selectedDayParts) {


                    $scope.priceModes = priceModes;
                    $scope.product = product;
                    $scope.code = { Suffix: '', DurationMax: 0, PriceMode: 1, ProductId: product.Id, DayParts: [] };

                    $scope.cancel = function () {
                        $modalInstance.close();
                    }

                    $scope.save = function () {
                        
                        addProductCode(createNewProductCode(product,$scope.code.Suffix, $scope.code.DurationMax, $scope.code.PriceMode));
                        $modalInstance.close();
                    }
                }],
                resolve: {
                    product: function () {
                        return $scope.data.channel.selectedProduct
                    },
                    priceModes: function () {
                        return $scope.priceModes;
                    },
                    channel: function () {
                        return $scope.data.channel;
                    },
                    addProductCode: function ()
                    {
                        return addProductCode;
                    },
                    selectedDayParts: function () {
                        return $scope.data.selectedDayParts;
                    }
                }
            });
        }

        function addProductCode(code)
        {
            $scope.data.pendingSave = true;
            $scope.data.channel.selectedProduct.Codes.push(code);
        }

        $scope.dataChanged = function (code) {
            $scope.data.pendingSave = true;
            code.Changed = true;
            /*var product = _.find($scope.data.channel.Products, { Id: code.ProductId });
            if (product != null)
            {
                var c = _.find(product.Codes, { Id: code.Id });
                if (c != null)
                    c.Changed = true;
            }*/
        };

        function CollectProductDayParts() {
            var result = [];					

            $scope.data.channel.Products.forEach(function (p) {
                _.filter(p.Codes, { Changed: true }).forEach(function (c) {
                    c.DayParts.forEach(function (pdp) {
                        if(pdp.Id == null)
                        {
                            //new
                            pdp.ChannelId = $scope.data.channel.Channel.Id;
                            pdp.ProductId = p.Id;                            
                        }
                        pdp.DurationMax = c.DurationMax;
                        pdp.ProductCode = c.Code;
                        pdp.PriceMode = c.PriceMode;
                        if ($scope.data.dayPartsUsageCount[pdp.DayPartId].count <= 0)
                            //If part not used on day/week matrix, set price to 0
                            pdp.Price = 0;
                        result.push(pdp);
                    });
                });
            });
            return result;
        }

        $scope.selectedDayParts = function () {
            var result = [];
            for (var key in $scope.data.dayPartsUsageCount) {
                if ($scope.data.dayPartsUsageCount[key].count > 0)
                    result.push($scope.data.dayPartsUsageCount[key].part);
            }
            return _.sortBy(result, 'Id');
        };

        $scope.getCodeDayParts = function (c) {
            return _.filter(c.DayParts, function (d) {
                return $scope.data.dayPartsUsageCount[d.DayPartId].count > 0;
            });
        };

        function updateNewProductDayParts(productDayParts)
        {
            $scope.data.channel.Products.forEach(function (p) {
                _.filter(p.Codes, { Changed: true }).forEach(function (c) {
                    c.DayParts.forEach(function (pdp) {
                        if (pdp.Id == null) {
                            var newRecord = _.find(productDayParts, function (part) {
                                return part.DayPartId == pdp.DayPartId && part.ProductId == pdp.ProductId && part.ProductCode == pdp.ProductCode;
                            });
                            if (newRecord != null)
                                pdp.Id = newRecord.Id;
                        }

                    });
                });
            });
        }

        $scope.formatHour = function (index)
        {
            var s1 = index.toString(), s2 = (index + 1).toString();
            return _.padLeft(s1, 2, '0') + '-' + _.padLeft(s2, 2, '0');
        }

        $scope.orderCodeBy = function (c)
        {
            var iSuffix = getSuffix(c);
            if (iSuffix != null)
                return iSuffix;
            var duration = parseInt(c.DurationMax);
            if (isNaN(duration))
                duration = 0;
            return duration;
        }

        function getSuffix(c)
        {
            var suffix = null//c.Suffix;
            if (suffix == null)
            {
                var index = c.Code.indexOf('-');
                if (index >= 0)
                    suffix = c.Code.substring(index + 1);
            }
            var nSuffix = parseInt(suffix);
            if (!isNaN(nSuffix))
                return nSuffix;            
            return null;
        }

        function addNewCodeRow(code, index)
        {
            var increment = 5;
            if (index > 1)
            {
                var sortedCodes = _.sortBy($scope.data.channel.selectedProduct.Codes, function (c) {
                    return $scope.orderCodeBy(c);
                });
				//Override default increment if last two records have different increment
                increment = parseInt($scope.orderCodeBy(sortedCodes[index]) - $scope.orderCodeBy(sortedCodes[index-1]));
                if (isNaN(increment))
                    increment = 5;
            }                
            var iSuffix = getSuffix(code);
            var newSuffix = '';
            if (iSuffix != null) {
                iSuffix += increment;
                newSuffix = iSuffix.toString();
            }
            var duration = parseInt(code.DurationMax);
            if (isNaN(duration))
                duration = 0;
            duration += increment;
            var newCode = createNewProductCode($scope.data.channel.selectedProduct, newSuffix, duration, code.PriceMode);
            addProductCode(newCode);
        }

        $scope.checkKey = function (event, code, rowIndex, colIndex) {
			//which = 40 down arrow, 38 up arrow
            var columnCount = 3 + $scope.selectedDayParts().length;
            var textInputColumnCount = columnCount;	//one hidden input at the end
            var timeout = 0;
            if (event.which == 40 || event.which == 13)
            {
				//down arrow
                if (rowIndex == $scope.data.channel.selectedProduct.Codes.length - 1 && colIndex != columnCount - 1) {
                    addNewCodeRow(code, rowIndex);
                    timeout = 200;
                }
                $timeout(function () {
                    var position = (rowIndex+1) * textInputColumnCount + (event.which == 40 ? colIndex : 0);
                    document.querySelectorAll('input')[position].focus();
                }, timeout);
            }
            if (event.which == 38) {
                if (rowIndex != 0)
                {
                    $timeout(function () {
                        var position = (rowIndex - 1) * textInputColumnCount + colIndex;
                        document.querySelectorAll('input')[position].focus();
                    }, timeout);
                }
                
            }
            if ((event.which == 9) && colIndex == -1)
            {
                
                var selector = 'input';
                var position;
                if (!event.shiftKey)
                {
                    //tab on last hidden input
                    if (rowIndex == $scope.data.channel.selectedProduct.Codes.length - 1) {
                        addNewCodeRow(code, rowIndex);
                        timeout = 200;
                    }                    
                    position = (rowIndex + 1) * textInputColumnCount;
                }
                else
                {
                    position = rowIndex;
                    selector = 'select';
                }
				
                $timeout(function () {                    
                    document.querySelectorAll(selector)[position].focus();
                }, timeout);
            }
            				
        };

   //     $scope.checkCodeGridTab = function (event, code,rowIndex,operation) {
			////operation = 0 keydown 1 keyup
   //         var textInputColumnCount = 2 + $scope.selectedDayParts().length;
   //         if (event.which = 9 && !event.shiftKey)
   //         {
   //             event.stopPropagation();
   //             if (!tabDown)
			//	  tabDown = true;
   //             else
   //             {
   //                 tabDown = false;
   //                 if (rowIndex == $scope.channel.selectedProduct.Codes.length - 1)
   //                 {
   //                     addNewCodeRow(code, rowIndex);
   //                     $timeout(function () {
   //                         var position = (rowIndex + 1) * textInputColumnCount;
   //                         document.querySelectorAll('input')[position].focus();
   //                     }, 200);
   //                 }
                    
   //             }
					
   //         }
   //     };

        

        $scope.removeCode = function (c) {
            var criteria = c.Id != null ? { Id: c.Id } : { Code: c.Code };
            c.DayParts.forEach(function (pdp) {
				if(pdp.Id != null)
                    $scope.data.removedDayPartsIds.push(pdp.Id);
            });
            _.remove($scope.data.channel.selectedProduct.Codes, criteria);
            $scope.data.pendingSave = true;	
        };

        var initPageLoad = _.once(init);

        function init() {
            if ($scope.data.channel == null || shouldLoadChannel) {
                loadChannel();
            }            
        }

        $scope.changeTab = function (tab)
        {
            if (tab == 'week') {
                $scope.weekActive = true;
                $scope.productsActive = false;
            }
            else {
                $scope.weekActive = false;
                $scope.productsActive = true;
            }		  
            var urlParts = location.href.split('/');
            var newUrl;
            var lastUrlPart = urlParts[urlParts.length - 1];
            if (lastUrlPart == 'week' || lastUrlPart == 'products')
                newUrl = urlParts.slice(0, -1).join('/');
            else
                newUrl = urlParts.join('/');
            newUrl += '/' + tab;
			
            if (lastUrlPart != tab)
                window.history.replaceState(null, null, newUrl);

        }

        if ($routeParams.tab)
            $scope.changeTab($routeParams.tab);

        initPageLoad();

        function uuidv4() {
            return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
                var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
                return v.toString(16);
            });
        }
    }]);
