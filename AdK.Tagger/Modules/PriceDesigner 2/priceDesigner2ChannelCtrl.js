angular.module('app')
	.controller('priceDesigner2ChannelCtrl', ['$scope', '$filter', '$modal', '$routeParams', '$timeout', '$location',
		'confirmPopup', 'infoModal', 'Service', 'priceDesigner2Service','yesnoPopup',
		function ($scope, $filter, $modal, $routeParams, $timeout, $location, confirmPopup, infoModal, Service, priceDesigner2Service,yesnoPopup) {
			$scope.id = $routeParams.id;

			$scope.data = priceDesigner2Service;

			var shouldLoadChannel = false;

			$scope.hours = [];
			$scope.days = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
			
			var selectedCells = [];
			var firstCell = {};
			var selectedRange = {};
		

			if (priceDesigner2Service.Id != $scope.id) {
				shouldLoadChannel = true;            
				priceDesigner2Service.init();
				priceDesigner2Service.Id = $scope.id;
			}
		
			$scope.priceModes = [{ Id: 1, Name: 'Per second' }, { Id: 2, Name: 'Exact' }];
			var tabDown = false;
        
			$scope.weekActive = true;
			$scope.productsActive = false;                        

			function loadChannel() {
				Service('GetChannelWithPriceDefinitions2', { channelId: $scope.id }).then(function (channel) {
					$scope.data.channel = channel;
					//$scope.channel = priceDesigner11Service.channel;
                
					$scope.data.channel.Products.forEach(function (p) {
						p.Codes = _.filter(channel.ProductCodes, { ProductId: p.Id });
					});
					buildPriceDefinitions(channel);

					Service('GetProducts').then(function (products) {
						$scope.data.AllProducts = products;
						//$scope.AllProducts = priceDesigner11Service.AllProducts;
					});

					if ($scope.data.channel.Products.length > 0)
						$scope.data.channel.selectedProduct = $scope.data.channel.Products[0];
					else
						$scope.data.channel.selectedProduct = null;
					$scope.data.pendingSave = false;
                
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

			$scope.cellClicked = function (day, hour, $event) {

				deselectAllDayPartCells();

				if ($event.shiftKey) {//If shift+click then select rectangle
					if (selectedRange && selectedRange.dayFrom >= 0) {
						var startHour = selectedRange.hourFrom;
						var endHour = hour;
						var startDay = selectedRange.dayFrom;
						var endDay = day;
						if (hour < selectedRange.hourFrom) {
							startHour = hour;
							endHour = selectedRange.hourFrom;
						}
						if (day < selectedRange.dayFrom) {
							startDay = day;
							endDay = selectedRange.dayFrom;
						}
						selectedRange.dayFrom = startDay;
						selectedRange.dayTo = endDay;
						selectedRange.hourFrom = startHour;
						selectedRange.hourTo = endHour;
					}
                
				}
				else {

					selectedRange = { dayFrom: day, hourFrom: hour, dayTo: day, hourTo: hour };

				}
                        
            
			}

			function deselectAllDayPartCells() {

				selectedRanges = [];
			}

			$scope.dayPartRowClicked = function (d) {
				$scope.data.channel.DayParts.forEach(function (dp) {
					dp.Selected = false;
				});
				d.Selected = true;
				$scope.data.selectedDayPart = d;
			};

			var newDefId = -1;

			$scope.assign = function () {
				$scope.data.pendingSave = true;

				if (selectedRange && selectedRange.dayFrom >= 0) {
					for (var h = selectedRange.hourFrom; h <= selectedRange.hourTo; h++) {
						for (var d = selectedRange.dayFrom; d <= selectedRange.dayTo; d++) {
							var defs = $scope.getDefinitions(d, h);
							if (defs.length == 0 || (defs.length == 1 && defs[0].from == 0 && defs[0].to == 3599)) {
								//Only add part if empty or there is only one full part - don't handle partial parts for now
								
								var def = {
									source: {
										Day: d,
										time_from: `${h}:00:00`,
										time_to: `${h}:59:59`,
										DayPartId: $scope.data.selectedDayPart.Id,
										channelId: $scope.data.channel.Channel.Id,
										DayPart: JSON.parse(JSON.stringify($scope.data.selectedDayPart)),
										From: {},
										To: {}
									},
									from: 0,
									to: 3599,
									hour: h,
									id: newDefId--
								};
								setTimeSpan(def.source.From, def.hour, def.from);
								setTimeSpan(def.source.To, def.hour, def.to);
								if (defs.length > 0) {
									defs[0].source.DayPartId = def.source.DayPartId;
									defs[0].source.DayPart = def.source.DayPart;
									defs[0].source.modified = true;
								}									
								else
									defs.push(def);

								var newId = $scope.data.selectedDayPart.Id;
								if (newId in $scope.data.dayPartsUsageCount)
									$scope.data.dayPartsUsageCount[newId].count++;
								else {
									$scope.data.dayPartsUsageCount[newId] = { part: def.source.DayPart, count: 1 };
									//add to product codes
									addPartToProductCodes($scope.data.selectedDayPart);
								}
							}
							
						}
					}
				}

			};

			$scope.unassign = function () {
				$scope.data.pendingSave = true;
				if (selectedRange && selectedRange.dayFrom >= 0) {
					for (var h = selectedRange.hourFrom; h <= selectedRange.hourTo; h++) {
						for (var d = selectedRange.dayFrom; d <= selectedRange.dayTo; d++) {
							for (var def of $scope.getDefinitions(d, h)) {
								def.markDeleted = true;
							}
						}
					}
				}
			}

			$scope.enableAddHourButton = function () {
				return $scope.data.selectedDayPart != null && selectedRange && selectedRange.dayFrom >= 0;
			};

			$scope.enableRemoveHourButton = function () {
				var rangeSelected = selectedRange && selectedRange.dayFrom >= 0;
				if (rangeSelected) {
					for (var h = selectedRange.hourFrom; h <= selectedRange.hourTo; h++) {
						for (var d = selectedRange.dayFrom; d <= selectedRange.dayTo; d++) {
							if ($scope.getDefinitions(d, h).length > 0) {
								return true;
							}
						}
					}					
				}
				return false;				
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

						$scope.dp = dp != null ? Object.assign({}, dp) : {};
						$scope.title = dp != null ? 'Edit' : 'Add new';

						$scope.cancel = function () {
							$modalInstance.close();
						}

						$scope.save = function () {
							saveDayPart($scope.dp);
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

			function saveDayPart(dp) {
				var sets = $scope.data.channel.DayPartSets;
				if (sets == null || sets.length == 0)
					Service('CreateDayPartSet', { name: 'My Day parts' }).then(function (set) {
						sets.push(set);
						dp.day_part_set_id = set.Id;
						savePart(dp);
					});
				else {
					dp.day_part_set_id = sets[0].Id;
					savePart(dp);
				}
					
			}

			function savePart(dp) {
				Service(!dp.Id ? 'CreateDayPart' : 'UpdateDayPart', { setId: dp.day_part_set_id, dayPart: dp }).then(function (res) {
					if (!dp.Id)
					{
						$scope.data.channel.DayParts.push(res);
						addPartToProductCodes(res);
					}
					else
					{
						var dayp = _.find($scope.data.channel.DayParts, { Id: dp.Id });
						if (dayp != null)
						{
							Object.assign(dayp, dp);
							var dictPriceDefinitions = priceDesigner2Service.dictPriceDefinitions;
							for (var key in dictPriceDefinitions) {
								var defs = dictPriceDefinitions[key];
								for (var def of defs) {
									if (def.source.DayPart.Id == dp.Id) {
										Object.assign(def.source.DayPart, dp);
									}
								}
							}
						}					

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

			
		        

			$scope.saveToDatabase = function () {
				confirmPopup.open('Save changes?', null, 'Do you really want to save all changes to the database?').then(function () {
					saveToDatabase(false);
				});
			};

			function saveToDatabase(goBack) {
				//Extract new and changed definitions
				var priceDefs = [];
				var deletedDefIds = {};
				var dictPriceDefinitions = priceDesigner2Service.dictPriceDefinitions;
				for (var key in dictPriceDefinitions) {
					if (dictPriceDefinitions[key].length > 0) {
						for (var i = 0; i < dictPriceDefinitions[key].length; i++) {
							var def = dictPriceDefinitions[key][i];
							if (!def.markDeleted) {
								if (def.source.Id == null || def.source.modified) {
									var obj = JSON.parse(JSON.stringify(def));
									obj.source.DayPart = null;
									delete obj.source.__type;
									if (obj.source.Id == null)
										delete obj.source.Id;
									priceDefs.push(obj);

									if (def.source.Id in deletedDefIds) {
										deletedDefIds[def.source.Id]--;
									} else {
										deletedDefIds[def.source.Id] = -1;
									}
								}
							} else if (def.source.Id > 0) {
								if (def.source.Id in deletedDefIds) {
									deletedDefIds[def.source.Id]++;
								} else {
									deletedDefIds[def.source.Id] = 1;
								}
							}
						}
					}
				}

				var deletedIds = [];
				for (var key in deletedDefIds) {
					if (deletedDefIds[key] > 0)
						deletedIds.push(key);
				}

				Service('SaveChannelPriceDefinitions2', {
					priceDefs: transformTimeStamps(getUniqueDbDefs(priceDefs.map(pd => pd.source))),
					deletedIds: deletedIds
				}).then(function (updatedDbDefs) {
					for (var i = 0; i < priceDefs.length; i++) {
						var pd = priceDefs[i];
						var dbDef = pd.source.Id > 0 ? updatedDbDefs.find(upd => upd.Id == pd.source.Id) : findDbDef(updatedDbDefs, pd.source);
						if (dbDef != null) {
							if (pd.source.Id > 0) {
								var index = $scope.data.channel.PriceDefinitions.findIndex(pd => pd.Id == dbDef.Id);
								if (index >= 0)
									$scope.data.channel.PriceDefinitions[index] = dbDef;
							} else {
								var existing = findDbDef($scope.data.channel.PriceDefinitions, dbDef);
								if (existing != null)
									Object.assign(existing, dbDef);
								else
									$scope.data.channel.PriceDefinitions.push(dbDef);
							}
							pd.source = dbDef;
						}
					}

					for (var key in dictPriceDefinitions) {
						if (dictPriceDefinitions[key].length > 0) {
							dictPriceDefinitions[key] = dictPriceDefinitions[key].filter(d => d.markDeleted != true);
						}
					}

					Service('SaveChannelProductDayParts', { productDayParts: CollectProductDayParts(), deleted: $scope.data.removedDayPartsIds }).then(function (productDayParts) {
						updateNewProductDayParts(productDayParts);
						$scope.data.removedDayPartsIds = [];
						$scope.data.pendingSave = false;
						if (goBack)
							history.back();
					});


				});
			}

			$scope.revert = function () {
				loadChannel();
			}

			function compareTimeStamps(ts1, ts2) {
				return (ts1.Hours - ts2.Hours)*3600 + (ts1.Minutes - ts2.Minutes)*60 + ts1.Seconds - ts2.Seconds;
			}

			function serializeTimeStamp(ts) {
				return `${ts.Hours}:${ts.Minutes}:${ts.Seconds}`;
			}

			function transformTimeStamps(dbDefs) {
				var result = [];
				for (var i = 0; i < dbDefs.length; i++) {
					var dbDef = dbDefs[i];
					var obj = Object.assign({}, dbDef);
					obj.From = serializeTimeStamp(obj.From);
					obj.To = serializeTimeStamp(obj.To);
					result.push(obj);
				}
				
				return result;
			}

			function getUniqueDbDefs(dbPriceDefs) {
				var result = [];
				for (var i = 0; i < dbPriceDefs.length; i++) {
					var def = dbPriceDefs[i];
					if (findDbDef(result, def) == null) {
						result.push(def);
					} 
				}
				return result;
			}

			function findDbDef(priceDefs, def) {
				return priceDefs.find(pd => compareDbDefs(pd, def) == 0);
			}

			$scope.productSelected = function (p) {
				$scope.data.channel.selectedProduct = p;
			};

			function compareDbDefs(def1, def2) {
				return (def1.Day - def2.Day) * 86400 + compareTimeStamps(def1.From, def2.From);					
			}

			function setTimeSpan(ts, hour, seconds) {
				ts.Hours = hour;
				ts.Minutes = Math.floor(seconds / 60);
				ts.Seconds = seconds % 60;
			}


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
					if(d.DayPartId in $scope.data.dayPartsUsageCount)
						return $scope.data.dayPartsUsageCount[d.DayPartId].count > 0;
					return false;
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

			$scope.formatHour = function (hour)
			{
				var s1 = hour.toString(), s2 = (hour + 1).toString();
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
				if ($scope.hours.length == 0) {
					for (var i = 0; i <= 23; i++) {
						$scope.hours.push(i);
					}
				
				}
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

			

			function buildPriceDefinitions(channel) {
				priceDesigner2Service.dictPriceDefinitions = {};
				var dictPriceDefinitions = priceDesigner2Service.dictPriceDefinitions;
				var id = 1;
				for (var i = 0; i < channel.PriceDefinitions.length; i++) {
					var pd = channel.PriceDefinitions[i];
					/*var mFrom = getFullMomentFromTimeSpan(pd.From);
					var mTo = getFullMomentFromTimeSpan(pd.To);*/
					for (var h = pd.From.Hours; h <= pd.To.Hours; h++) {
						var key = getPriceDefDictKey(pd.Day, h);
						var dayHour;
						if (key in dictPriceDefinitions) {
							dayHour = dictPriceDefinitions[key];
						} else {
							dayHour = [];
							dictPriceDefinitions[key] = dayHour;
						}
						var pd_hour = { id: id++, source: pd, from: getFrom(pd.From,h), to: getTo(pd.To, h), hour: h };
						dayHour.push(pd_hour);
					}
					if (pd.DayPartId in $scope.data.dayPartsUsageCount)
						$scope.data.dayPartsUsageCount[pd.DayPartId].count++;
					else {
						$scope.data.dayPartsUsageCount[pd.DayPartId] = { part: pd.DayPart, count: 1 };
					}
				}
			}

			function getFullMomentFromTimeSpan(span) {
				return moment().startOf('day').add(span);
			}

			function getFrom(mFrom, hour) {
				if (mFrom.Hours == hour) {
					return mFrom.Minutes * 60 + mFrom.Seconds;
				} else {
					return 0;
				}
			}

			function getTo(mTo, hour) {
				if (mTo.Hours == hour) {
					return mTo.Minutes * 60 + mTo.Seconds;
				} else {
					return 3599;
				}
			}

			$scope.getCellSelected = function (day, hour) {
				return selectedRange && day >= selectedRange.dayFrom && day <= selectedRange.dayTo &&
					hour >= selectedRange.hourFrom && hour <= selectedRange.hourTo;
			};

			function getPriceDefDictKey(day, hour) {
				return day.toString() + '_' + hour.toString()
			}

			$scope.getDefinitions = function (day, hour) {
				var key = getPriceDefDictKey(day, hour);
				var dictPriceDefinitions = priceDesigner2Service.dictPriceDefinitions;
				if (!(key in dictPriceDefinitions)) {
					dictPriceDefinitions[key] = [];				
				}
				return dictPriceDefinitions[key];
			};

			$scope.getDefinitionStyle = function (def) {
				return {
					color: def.source.DayPart.Color,
					backgroundColor: def.source.DayPart.BackgroundColor,
					width: Math.round((def.to - def.from) / 3600 * 100, 0) + '%',
					left: Math.round(def.from / 3600 * 100, 0) + '%',
					height: '25px',
					top: '-2px'
				};
			};

			$scope.getDefinitionDescription = function (def) {
				var code = def.source.DayPart.Short_code;
				var from = moment().startOf('day').add(def.hour, 'hours').add(def.from, 'seconds').format('HH:mm:ss');
				var to = moment().startOf('day').add(def.hour, 'hours').add(def.to + 1, 'seconds').format('HH:mm:ss');
				return `${code} ${from}-${to}`;
			};

			$scope.filterDefinitions = function (def) {
				return !def.markDeleted;
			}

			

			$scope.cellDblClick = function (day, hour) {
				var definitions = $scope.getDefinitions(day, hour);
				
				if (definitions.length > 0) {
					openDefinitionsModal(day, hour, $scope.data.channel.DayParts).result.then(function (parts) {
						var definitions = getPrevCurrentNextDefinitions(day, hour);
						var maxPart = _.max(parts, 'id');
						var maxId = (maxPart.id | 0) + 1;
						for (var i = 0; i < parts.length; i++) {
							var part = parts[i];
							var def;
							if (part.markDeleted) {
								if (part.id > 0)
									def = definitions.find(d => d.id == part.id);
								else
									def = definitions.find(d => compareDbDefs(d.source, part.source) == 0);
								if (def != null) {
									def.markDeleted = true;
									$scope.data.pendingSave = true;
								}
									
							} else {
								if (part.id == null || part.source.modified) {
									$scope.data.pendingSave = true;
									if (part.id != null) {
										var def = definitions.find(d => d.id == part.id);										
										if (def != null) {
											Object.assign(def, part);
										}
									} else {
										//part.source.Id = newDefId--;
										part.id = maxId++;
										priceDesigner2Service.dictPriceDefinitions[getPriceDefDictKey(day, part.hour)].push(part);
									}
								}
							}
							
						}
					},
						function () {
						});
				} else {
					infoModal.open('Info', 'No day parts defined for the slot');
				}

			}

			function getPrevCurrentNextDefinitions(day, hour) {
				var current = $scope.getDefinitions(day, hour);
				var previous = [], next = [];
				if (hour > 0)
					previous = $scope.getDefinitions(day, hour - 1);
				if (hour < 23)
					next = $scope.getDefinitions(day, hour + 1);
				return previous.concat(current).concat(next);
			}

			
			function openDefinitionsModal(day, hour, dayParts) {
				var data = getPrevCurrentNextDefinitions(day, hour);
				var modalInstance = $modal.open({
					animation: false,
					templateUrl: 'dayHour.html',
					size: 'lg',
					controller: ['$scope', '$modalInstance', 'data', 'dayParts', 'functions', function ($scope, $modalInstance, data, dayParts, functions) {

						$scope.sortParts = function (data) {
							return data.sort((a, b) => {
								return (a.hour - b.hour) * 10000 + a.from - b.from;
							});
						}

						$scope.insertParams = {
							from: null,
							to: null,
							dayPartId: null
						}

						
						for (var i = 0; i < data.length; i++) {
							if(!data[i].markDeleted)
								data[i].markDeleted = false;
						}
						$scope.data = $scope.sortParts(JSON.parse(JSON.stringify(data)));
						
						$scope.dayParts = dayParts;

						$scope.splitActive = false;

						$scope.cancel = function () {
							$modalInstance.dismiss();
						}

						$scope.save = function () {
							$modalInstance.close($scope.data);
						}

						$scope.getTimeFormatted = function (hour, time) {
							//return moment().startOf('day').add(hour, 'hours').add(time, 'seconds').format('HH:mm:ss');
							var minutes = Math.floor(time / 60);
							if (minutes == 60) {
								hour++;
								minutes = 0;
							}
							return `${hour.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${(time % 60).toString().padStart(2, '0')}`;
						}

						$scope.split = function (part) {
							part.splitActive = true;
							$scope.splitActive = true;
							part.splitDayPartId1 = part.source.DayPartId;
							part.splitDayPartId2 = part.source.DayPartId;
							part.splitValue = part.from + Math.round((part.to + 1 - part.from) / 2);
						}

						$scope.cancelSplit = function (part) {
							part.splitActive = false;
							$scope.splitActive = false;
							part.splitValue = 0;
						}

						$scope.confirmSplit = function (part) {
							var to = part.to;
							part.to = part.splitValue - 1;
							$scope.setTimeSpan(part.source.To, part.hour, part.to);
							part.source.DayPart = $scope.dayParts.find(dp => dp.Id == part.splitDayPartId1);
							part.source.DayPartId = part.splitDayPartId1;
							part.source.modified = true;
							var newPart = {
								from: part.splitValue,
								to: to,
								hour: part.hour,
								source: angular.copy(part.source),
								markDeleted: false
							};
							newPart.source.Id = null;
							$scope.setTimeSpan(newPart.source.From, newPart.hour, newPart.from);
							$scope.setTimeSpan(newPart.source.To, newPart.hour, newPart.to);
							newPart.source.DayPartId = part.splitDayPartId2;
							newPart.source.DayPart = $scope.dayParts.find(dp => dp.Id == part.splitDayPartId2);
							$scope.data = $scope.sortParts($scope.data.concat([newPart]));
							$scope.cancelSplit(part);
						}

						$scope.checkSplitActive = function () {
							return $scope.data.find(p => p.splitActive) != null;
						}

						$scope.getTimeSpan = function (hour, time) {
							return `${hour.toString().padStart(2, '0')}:${Math.floor(time / 60).toString().padStart(2, '0')}:${(time % 60).toString().padStart(2, '0')}`;
						}

						$scope.showInsert = function (index) {
							if (!$scope.data[index].Selected || index >= $scope.data.length - 1)
								return false;
							var nextIndex = $scope.data.findIndex((d,ix) =>!d.markDeleted && ix > index);
							if (nextIndex > 0)
								return $scope.data[nextIndex].Selected;
							return false;							
						}

						$scope.isCurrent = function (d) {
							return d.hour == hour;
						}

						$scope.insert = function (index) {
							$scope.insertPart1 = $scope.data[index];
							$scope.insertPart2 = $scope.data[index+1];
							$scope.insertParams.from = $scope.insertPart1.to + 1;
							$scope.insertParams.to = $scope.insertPart2.from;
							$scope.insertActive = true;
							$scope.insertPart1.insertActive = true;
						}

						$scope.cancelInsert = function (d) {
							d.insertActive = false;
							$scope.insertActive = false;
						}

						$scope.confirmInsert = function (d) {
							var part1 = $scope.insertPart1;
							var part2 = $scope.insertPart2;
							var source = {
								From: {},
								To: {},
								Day: part1.source.Day,
								DayPartId : $scope.insertParams.dayPartId,
								DayPart: $scope.dayParts.find(dp => dp.Id == $scope.insertParams.dayPartId),
								ChannelId: part1.source.ChannelId
							};
							$scope.setTimeSpan(source.From, part1.hour, $scope.insertParams.from);
							$scope.setTimeSpan(source.To, part2.hour, $scope.insertParams.to - 1);
							var newPart = {
								from: $scope.insertParams.from,
								to: part1.hour == part2.hour ? $scope.insertParams.to - 1 : 3599,
								hour: part1.hour,
								source: source,
								markDeleted: false
							};
							var arr = [newPart];
							if (part1.hour != part2.hour) {
								var newPart2 = {
									from: part2.from,
									to: $scope.insertParams.to - 1,
									hour: part2.hour,
									source: source,
									markDeleted: false
								};
								arr.push(newPart2);
							}

							part1.to = $scope.insertParams.from - 1;
							$scope.setTimeSpan(part1.source.To, part1.hour, part1.to);
							part1.source.modified = true;
							part2.from = $scope.insertParams.to;
							$scope.setTimeSpan(part2.source.From, part2.hour, part2.from);
							part2.source.modified = true;

							$scope.data = $scope.sortParts($scope.data.concat(arr));
							for (var i = 0; i < $scope.data.length; i++) {
								$scope.data[i].Selected = false;
							}
							$scope.cancelInsert(d);
							
						}

						$scope.getInsertLength = function () {
							return $scope.insertPart1.to + 1 - $scope.insertParams.from + $scope.insertParams.to - $scope.insertPart2.from;
						}

						$scope.merge = function (index) {
							$scope.mergeActive = true;
							$scope.mergePart1 = $scope.data[index];
							$scope.mergePart2 = $scope.data[index + 1];
							$scope.mergePart1.mergeActive = true;
						}

						$scope.confirmMerge = function (d, index) {
							var part1 = $scope.mergePart1;
							var part2 = $scope.mergePart2;
							
							/*for (var i = index+2; i < $scope.data.length; i++) {
								if (compareDbDefs($scope.data[i].source, part2.source) == 0) {
									if ($scope.data[i].hour == part2.hour)
										$scope.data[i].markDeleted = true;
									else
										$scope.data[i].source = part1.source;
								}
							}*/
							var partToChange = part1;
							var partToDelete = part2;
							if (part2.source.To.Hours != part1.source.To.Hours) {
								var nextIndex = $scope.data.findIndex((d, ix) => functions.compareDbDefs(d.source, part2.source) == 0 && !d.markDeleted && ix > index + 1);
								if (nextIndex >= 0) {
									var nextSource = $scope.data[nextIndex].source;									
									functions.setTimeSpan(nextSource.From, part2.source.To.Hours, 0);
									setTimeSpan(part2.source.To, part1.source.To.Hours, 3599);
									nextSource.modified = true;
								}
								

							} else if (part1.source.From.Hours != part2.source.From.Hours) {
								var prevIndex = $scope.data.findIndex((d, ix) => functions.compareDbDefs(d.source, part1.source) == 0 && !d.markDeleted && ix < index);
								if (prevIndex >= 0) {
									var prevSource = $scope.data[prevIndex].source;
									functions.setTimeSpan(prevSource.To, part1.source.From.Hours, 3599);
									prevSource.modified = true;
								}
								partToChange = part2;
								partToDelete = part1;
							} else {
								
								
								
							}
							partToChange.source.modified = true;
							partToChange.source.DayPartId = d.mergeDayPartId;
							partToChange.source.DayPart = $scope.dayParts.find(dp => dp.Id == d.mergeDayPartId);
							partToDelete.markDeleted = true;
							if (partToChange == part1) {
								part1.to = part2.to;
								functions.setTimeSpan(part1.source.To, part1.hour, part2.to);
							} else {
								part2.from = part1.from;
								functions.setTimeSpan(part2.source.From, part1.hour, part1.from);
							}
								
							for (var i = 0; i < $scope.data.length; i++) {
								$scope.data[i].Selected = false;
							}
							$scope.cancelMerge(d);
						}

						$scope.cancelMerge = function (d) {
							d.mergeActive = false;
							$scope.mergeActive = false;
						}

						$scope.showMerge = function (index) {
							if (!$scope.data[index].Selected || index >= $scope.data.length - 1)
								return false;
							var nextIndex = $scope.data.findIndex((d, ix) => !d.markDeleted && ix > index);
							if (nextIndex > 0)
								return $scope.data[nextIndex].Selected && $scope.data[index].hour == $scope.data[nextIndex].hour;
							return false;
						}
						

						$scope.setTimeSpan = function (ts, hour, seconds) {
							functions.setTimeSpan(ts, hour, seconds);
						}

						$scope.delete = function (part) {
							var index = $scope.data.findIndex(d => d.id == part.id);
							if (index > 0) {
								//Check previous part
								var previousPart = $scope.data[index - 1];
								if (previousPart.source.Id > 0 && previousPart.source.Id == part.source.Id) {
									previousPart.source.modified = true;
									previousPart.to = part.from == 0 ? 3599 : part.from - 1;
									$scope.setTimeSpan(previousPart.source.To, previousPart.hour, previousPart.to); 
								}
							}
							part.markDeleted = true;
							if (index < $scope.data.length - 1) {
								//Check next
								var nextPart = $scope.data[index + 1];
								if (nextPart.source.Id > 0 && nextPart.source.Id == part.source.Id) {
									nextPart.source.modified = true;
									nextPart.from = part.to == 3599 ? 0 : part.to + 1;
									$scope.setTimeSpan(nextPart.source.From, nextPart.hour, nextPart.from);
								}
							}
						}

					}],
					resolve: {
						data: function () {
							return data;
						},
						dayParts: function () {
							return dayParts;
						},
						functions: function () {
							return {
								compareDbDefs: compareDbDefs,
								setTimeSpan: setTimeSpan
							}
						}
					}
				});
				return modalInstance;
			}

			$scope.backToChannel = function () {
				if (!$scope.data.pendingSave) {
					history.back();
				} else {
					yesnoPopup.open('Save changes?', null, 'You have unsaved changes. Save them prior to going back to channel list?').then(
						function (result) {
							if (result == 'yes') {
								saveToDatabase(true);
							} else {
								history.back();
							}
						}
					)
				}
			}
    }]);
