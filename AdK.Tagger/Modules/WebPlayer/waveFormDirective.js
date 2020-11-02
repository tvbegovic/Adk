angular.module('app')
	.directive('waveForm', ['$timeout', 'webPlayerFactory', function ($timeout, webPlayerFactory) {
		return {
			restrict: 'E',
			scope: {
				data: '=',
				index: '=',
				width: '=',
				height: '=',
				playing: '=',
				onClick: '&',
				onDblclick: '&',
				layer: '=',
				isActive: '=',
				onSegmentResized: '&',
				waveData: '=', 	//segmentResizing sequence of the segment that is currently resizing  resizingEdge

				onSegmentStartResizing: '&',
				/*onSegmentEndResizing: '&',*/
				onMouseEnter: '&',
				resizeElementChanged: '&',
				requestChunksRender: '&'
			},
			link: function ($scope, element, attrs) {
				var canvas = document.createElement('canvas');
				canvas.width = $scope.width;
				canvas.height = $scope.height;
				canvas.onclick = onClick;
				canvas.ondblclick = onDblclick;
				element[0].appendChild(canvas);

				canvas.addEventListener('mousedown', canvasMouseDown.bind($scope));
				canvas.addEventListener('mousemove', canvasMouseMove.bind($scope));
				canvas.addEventListener('mouseup', canvasMouseUp.bind($scope));
				canvas.addEventListener('mouseleave', canvasMouseLeave.bind($scope));
				canvas.addEventListener('mouseenter', canvasMouseEnter.bind($scope));

				var indicatorWidth = 1;
				var indicatorColor = '#FFF';
				var imagesRendered = {};	//dictionary cache for images
				var loadedFlagSum = 0;
				var segmentDivs = {};	//dictionary key=layer
				var fullSegments = {};	//layer = key, indicates if segment is across full div
				var segmentCssClassBasic = 'wp_waveform_segment';

				var canvasX = null;
				var canvasY = null;
				var resizePosTolerance = 3;
				var resizeElement = null;
				var segmentId = null;
				//$scope.resizingEdge = null; //1 - right 2 - left
				var resizingEdgeRight = 1, resizingEdgeLeft = 2;
				var resizeHandler = null;
				var segmentStartDelta = 0;
				var segmentEndDelta = 0;
				var debug = false; //true;
				var debugFilter = new RegExp(/(renderBlock)|(canvasMouseLeave leaving up)+/, 'g'); //null;
				var fillDirectionRight = 1, fillDirectionLeft = 2;
				var canvasWidth;

				$scope.$watch((scope) => {
					return getLoadedFlagSum(scope);
				},
					(newVal, oldValue, scope) => {
						if (newVal > 0 && newVal != loadedFlagSum) {
							loadedFlagSum = newVal;
							requestAnimationFrame(drawImages.bind(scope, scope.data, scope.height));
						}
					});

				$scope.$watch('data',
					(newValue, oldValue, scope) => {
						canvas.width = scope.width;
						canvas.height = scope.height;
						scope.data.fillDiv = fillDiv;
						canvasWidth = scope.width;
						imagesRendered = {};
						var flags = getLoadedFlagSum(scope);
						if (flags > 0) {
							requestAnimationFrame(drawImages.bind(scope, scope.data, scope.height));
						}

					});

				$scope.$watch(scope => {
					var result = 0;

					if (scope.data.segments != null) {
						for (var key in scope.data.segments) {
							for (var i = 0; i < scope.data.segments[key].length; i++) {
								result += scope.data.segments[key][i].width;
							}
						}
					}
					return result;
				},
					(newValue, oldValue, scope) => {
						if (newValue > 0 || oldValue > 0) {
							debuglog('$scope.$watch segment length change ... newValue: ' + newValue + ' oldValue: ' + oldValue + ' time: ' + scope.data.Time);
							requestAnimationFrame(handleAllSegments.bind(scope, scope.data.segments, element));
						}
					}
				);

				$scope.$watch(scope => {
					var result = '';

					if (scope.data.segments != null) {
						for (var key in scope.data.segments) {
							for (var i = 0; i < scope.data.segments[key].length; i++) {
								result += scope.data.segments[key][i].source.name;
							}
						}
					}
					return result;
				},
					(newValue, oldValue, scope) => {
						if (newValue.length > 0 || oldValue.length > 0) {
							debuglog('$scope.$watch segment name changed ... newValue: ' + newValue + ' oldValue: ' + oldValue + ' time: ' + scope.data.Time);
							requestAnimationFrame(handleLayerSegments.bind(scope, scope.data.segments[scope.layer], element, scope.layer));
						}
					}
				);

				$scope.$watch('layer',
					(newValue, oldValue, scope) => {
						if (newValue != null) {
							var divsOldLayer = element[0].getElementsByClassName('layer' + oldValue);
							for (var i = 0; i < divsOldLayer.length; i++) {
								divsOldLayer[i].style.display = 'none';
							}
							var divsNewLayer = element[0].getElementsByClassName('layer' + newValue);
							for (var i = 0; i < divsNewLayer.length; i++) {
								divsNewLayer[i].style.display = 'block';
							}
							if (!(newValue in fullSegments) || !fullSegments[newValue]) {
								canvas.className = removeClass(canvas.className, segmentCssClassBasic);
							} else {
								canvas.className = addClass(canvas.className, segmentCssClassBasic);
							}
						}
					}
				)

				$scope.$watch('isActive',
					(newValue, oldValue, scope) => {
						//if (newValue != null && newValue != oldValue) {
						//var canvas = getCanvasElement();
						var ctx = canvas.getContext('2d');
						ctx.filter = !newValue ? 'brightness(50%)' : 'none';
						invalidateImages();
						requestAnimationFrame(drawImages.bind(scope, scope.data, scope.height));

					}
				)

				function invalidateImages() {
					for (var key in imagesRendered) {
						imagesRendered[key] = false;
					}
				}

				function handleAllSegments(segments, element) {
					for (var key in segments) {
						handleLayerSegments(segments[key], element, key);
					}
				}

				function handleLayerSegments(segments, element, layer) {

					if (!(layer in segmentDivs)) {
						segmentDivs[layer] = [];
					}
					debuglog('handleSegments');
					var pos = 0;
					var canvas = getCanvasElement();
					var blankIntervals = [];
					for (var i = 0; i < segments.length; i++) {

						var segment = segments[i];
						debuglog('WaveForm.handleSegments processing segment... start: ' + segment.start + ' width: ' + segment.width + ' Time: ' + $scope.data.Time);
						if (segment.start > pos)
							blankIntervals.push({ start: pos, end: segment.start });
						pos = segment.start;

						renderSegmentDiv(segment, layer);

						pos += segment.width;
					}
					if (pos < canvasWidth) {
						blankIntervals.push({ start: pos, end: canvasWidth });
					}

					if (blankIntervals.length == 1 && blankIntervals[0].start == 0 && blankIntervals[0].end == canvasWidth) {
						//special case - canvas is one segment
						if (canvas.className.length > 0) {
							var className = canvas.className;
							//Layout fix - write to property in another frame
							requestAnimationFrame(() => canvas.className = removeClass(className, segmentCssClassBasic));
						}						
						fullSegments[layer] = false;
						segmentId = null;
						var titleDiv = element[0].querySelector('.wp_waveform_segment_title');
						if (titleDiv != null && titleDiv.parentNode == element[0]) {
							titleDiv.parentNode.removeChild(titleDiv);
						}
					}

					//remove unused divs
					var toRemove = [];
					for (var i = 0; i < segmentDivs[layer].length; i++) {
						var div = segmentDivs[layer][i];
						var match = blankIntervals.find(i => div.offsetLeft >= i.start && div.offsetLeft + div.clientWidth <= i.end);
						if (match) {
							toRemove.push(i);
						}
					}
					for (var i = 0; i < toRemove.length; i++) {
						var ix = toRemove[i];
						var div = segmentDivs[layer][ix];
						debuglog('remove div left: ' + div.offsetLeft + ' width: ' + div.offsetWidth + ' Time: ' + $scope.data.Time);
						element[0].removeChild(div);
						segmentDivs[layer][ix] = null;
					}
					if(segmentDivs[layer].length > 0)
						segmentDivs[layer] = segmentDivs[layer].filter(d => d != null);

				}

				function renderSegmentDiv(segment, layer) {
					requestAnimationFrame(() => renderBlock(layer, segment.start, segment.width, segment.sequence, segment.source.name));
				}

				function renderBlock(layer, start, width, sequence, title) {
					var activeLayer = $scope.layer;
					debuglog('renderBlock- start: left: ' + start + ' width: ' + width + ' seq: ' + sequence + ' Time: ' + $scope.data.Time);
					var div = layer in segmentDivs ? segmentDivs[layer].find(d => getElementAttribute(d, 'segmentId') == sequence) : null;
					if (div == null) {
						debuglog('renderBlock div-not found: left: ' + start + ' width: ' + width + ' seq: ' + sequence + ' Time: ' + $scope.data.Time);
						//var textNode = createSegmentTextNode(title, layer);
						if (start > 0 || width < $scope.data.widthPx) {
							//create div only if not full segment (optimization)
							debuglog('renderBlock div-not full: ' + start + ' width: ' + width + ' seq: ' + sequence + ' Time: ' + $scope.data.Time);
							canvas.className = removeClass(canvas.className, segmentCssClassBasic);
							removeElementAttribute(canvas, 'segmentid');
							//remove text node from before
							var titleDiv = element[0].querySelector('.wp_waveform_segment_title');
							if (titleDiv != null && titleDiv.parentNode == element[0])
								element[0].removeChild(titleDiv);
							fullSegments[layer] = false;
							div = document.createElement('div');
							div.className = 'wp_waveform_segment layer' + layer;
							
							addElementAttribute(div, 'segmentId', sequence);
							//div.appendChild(textNode);
							addOrUpdateTextNode(div, title, layer);
							if (!(layer in segmentDivs))
								segmentDivs[layer] = [];
							addDiv(div, layer);
						} else {
							debuglog('renderBlock div-full: left: ' + start + ' width: ' + width + ' seq: ' + sequence + ' Time: ' + $scope.data.Time);
							if (!(layer in fullSegments) || !fullSegments[layer])
								fullSegments[layer] = true;
							if(layer == activeLayer)
								canvas.className = addClass(canvas.className, segmentCssClassBasic);
							addElementAttribute(canvas, 'segmentId', sequence);
							//element[0].appendChild(textNode)
							addOrUpdateTextNode(element[0], title, layer);
						}
					}
					if (div != null) {
						debuglog('renderBlock div-setting width and left: left: ' + start + ' width: ' + width + ' seq: ' + sequence + ' Time: ' + $scope.data.Time);
						if (width == $scope.data.widthPx) {
							//switch to full
							//var textNode = createSegmentTextNode(title, layer);
							canvas.className = addClass(canvas.className, segmentCssClassBasic);
							fullSegments[layer] = true;
							addElementAttribute(canvas, 'segmentId', sequence);
							addOrUpdateTextNode(element[0], title, layer);
							//element[0].appendChild(textNode);
							removeDiv(div, layer);
						} else {
							div.style.display = layer == activeLayer ? 'block' : 'none';
							div.style.left = start + 'px';
							addOrUpdateTextNode(div, title, layer);
							checkSegmentTitleVisibility(div, width);
							if (width >= 0)
								div.style.width = width + 'px';
						}
					}
					debuglog('renderBlock div-finished: left: ' + start + ' width: ' + width + ' seq: ' + sequence + ' Time: ' + $scope.data.Time);
					return div;
				}

				function addOrUpdateTextNode(parent, title, layer) {
					//var textNode = null;
					//for (var i = 0; i < parent.childNodes.length; i++) {
					//	var child = parent.childNodes[i];
					//	if (hasClass(null, 'wp_waveform_segment_title', child)) {
					//		textNode = child;
					//		child.innerText = title;
					//		break;
					//	}
					//}
					//if (textNode == null) {
					//	textNode = createSegmentTextNode(title, layer);
					//	parent.appendChild(textNode);
					//}
					webPlayerFactory.addOrUpdateTextNode(parent, title, layer, onClick, onDblclick);
						
				}

				function getCanvasElement() {
					return element[0].firstChild;
				}

				function addClass(classString, className, element) {
					return webPlayerFactory.addClass(classString, className, element);
				}

				function removeClass(classString, className, element) {
					return webPlayerFactory.removeClass(classString, className, element);
				}

				function hasClass(classString, className, element) {
					if (element)
						classString = element.className;
					var arr = classString.split(' ');
					return arr.indexOf(className) >= 0;						
				}

                function drawImages(data, height) {
                    var ctx = canvas.getContext('2d');
                    var pos = 0;
                    for (var i = 0; i < data.parts.length; i++) {
						var chunkData = data.parts[i];
						if (chunkData.waveForm.loaded) {
							if (!(i in imagesRendered) || !imagesRendered[i]) {
								imagesRendered[i] = true;
								var image = chunkData.waveForm.image;//data.segments == null ? chunkData.waveForm.image : chunkData.waveForm.altImage;
								ctx.drawImage(image,
									chunkData.offset,
									0,
									chunkData.width,
									height,
									pos,
									0,
									chunkData.width,
									height);
							}
						}
						pos += chunkData.width;
                        
                    }
				}

				function getLoadedFlagSum(scope) {
					var sum = 0;
					for (var i = 0; i < scope.data.parts.length; i++) {
						sum += scope.data.parts[i].waveForm.loaded == true ? 1 : 0;
					}
					return sum;
				}

                $scope.$watch('data.position',
                    (newValue, oldValue, scope) => {
                        requestAnimationFrame(drawPosition.bind(scope, scope.data, newValue, oldValue));
                    });

                function drawPosition(data, newValue, oldValue) {
                    var ctx = canvas.getContext('2d');
                    if (oldValue != null) {
                        //find old image to restore part that was overdrawn
                        var partStart = 0;
                        var partPos = 0;
                        var part = null;
                        for (var i = 0; i < data.parts.length; i++) {
                            part = data.parts[i];
                            if (oldValue < partStart + part.width) {
                                part = data.parts[i];
                                partPos = oldValue - partStart;
                                break;
                            }
                            partStart += part.width;
                        }
						if (part != null) {
							ctx.clearRect(oldValue, 0, indicatorWidth, this.height);
							if (part.waveForm.loaded) {
								ctx.drawImage(part.waveForm.image,
									part.offset + partPos,
									0,
									indicatorWidth,
									this.height,
									oldValue,
									0,
									indicatorWidth,
									this.height);
							}
							
						}
                        
                    }
                    if (newValue != null && newValue >= 0 && newValue < canvas.width) {
                        ctx.strokeStyle = indicatorColor;
                        ctx.beginPath();
                        ctx.moveTo(newValue + 0.5, 0);
                        ctx.lineTo(newValue + 0.5, this.height);
						ctx.stroke();
                    }
                    
				}

				function onClick(event) {
					var data = { x : getCanvasRelativeX(event), ctrlKey: event.ctrlKey };
					$scope.onClick({ data: data });
				}

				function addElementAttribute(element, key, value) {
					//var attr = document.createAttribute(key);
					//attr.value = value;
					//element.attributes.setNamedItem(attr);
					webPlayerFactory.addElementAttribute(element, key, value);
				}

				function removeElementAttribute(elem, key) {
					//var attr = elem.attributes.getNamedItem(key);
					//if (attr != null)
					//	elem.attributes.removeNamedItem(key);
					webPlayerFactory.removeElementAttribute(elem, key);
				}

				function getElementAttribute(elem, key) {
					//var attr = elem.attributes.getNamedItem(key);
					//if (attr != null)
					//	return attr.value;
					//return null;
					return webPlayerFactory.getElementAttribute(elem, key);
				}

				function segmentDivMouseDown(event) {
					
				}

				function segmentDivMouseMove(event) {
					//if (!resizing) {
					//	if (event.target.clientWidth - event.clientX <= 3)
					//		addClass(null, 'wp_resizing', event.target);
					//	else
					//		removeClass(null, 'wp_resizing', event.target);
					//}					
				}

				function canvasMouseDown(event) {
					var scope = this;
					debuglog('canvasMouseDown x: ' + event.clientX + ' y: ' + event.clientY + ' hasResizeElement: ' + (resizeElement != null) + ' Time: ' + scope.data.Time);
					if (resizeElement) {
						event.stopPropagation();
						var sequence = getElementAttribute(resizeElement, 'segmentId');
						scope.waveData.segmentResizing = sequence;
						if (scope.onSegmentStartResizing) {
							scope.onSegmentStartResizing({
								data: {
									sequence: getElementAttribute(resizeElement, 'segmentId'),
									posX: event.clientX - getCanvasX(),
									resizingEdge: checkOnEdge(event, scope).edge,
									elementX: resizeElement.offsetLeft,
									elementWidth: resizeElement.offsetWidth,
									chunkIndex: scope.index
								}
							});
							debuglog('canvasMouseDown: x: ' + event.clientX + ' y: ' + event.clientY + ' resizingEdge: ' + scope.waveData.resizingEdge + ' Time: ' + scope.data.Time);
						}
						//segmentStartDelta = 0;
						//segmentEndDelta = 0;
						
					}
				}

				function canvasMouseUp(event) {
					var scope = this;
					if (scope.waveData.segmentResizing) {
						debuglog('canvasMouseup x: ' + event.clientX + ' y: ' + event.clientY + ' sequence: ' + scope.waveData.segmentResizing + ' time: ' + scope.data.Time);
						event.stopPropagation();
						
						if (resizeElement) {
							if (scope.onSegmentResized) {
								scope.onSegmentResized({
									data: {
										seq: scope.waveData.segmentResizing,
										start: resizeElement.offsetLeft,
										width: resizeElement.offsetWidth,
										edge: scope.waveData.resizingEdge
									}
								});
							}						
						}							
						
					}
						
				}

				function canvasMouseMove(event) {
					var scope = this;
					var layer = scope.layer;
					if (!scope.waveData.segmentResizing) {
						if (getSegments(scope.data, layer) != null) {
							var result = checkOnEdge(event, scope);	//return { bool, element}
							if (result.edge) {
								resizeElement = result.element;
								debuglog('canvasMouseMove - on edge. Resizing edge: ' + result.edge);
								//scope.resizingEdge = result.edge;
								addClass(null, 'wp_resizing', event.target);
							} else {
								resizeElement = null;
								
								removeClass(null, 'wp_resizing', event.target);
							}
						}						
					} else if (!resizeHandler) {
						var posX, width;
						//var full = layer in fullSegments && fullSegments[layer] == true;
						if (resizeElement == canvas) {
							//add div and turn off fullSegments
							removeClass(null, segmentCssClassBasic, canvas);							
							if (scope.waveData.resizingEdge == resizingEdgeLeft) {
								posX = event.clientX - getCanvasX();
								width = canvas.width - posX;
							} else if (scope.waveData.resizingEdge == resizingEdgeRight) {
								posX = 0;
								width = event.clientX - getCanvasX();
							} else {
								debuglog('canvasMouseMove resizingEdge null. Time: ' + scope.Time);
							}
							requestAnimationFrame(() => {
								renderElement = renderBlock(layer, posX, width, scope.waveData.segmentResizing, findSegmentTitle(layer, scope.waveData.segmentResizing));
							});
								
						} else {
							var relativePosx = event.clientX - getCanvasX();
							var div = resizeElement;
							
							var resize = false;
							if (div != null) {
								if (scope.waveData.resizingEdge == resizingEdgeRight) {
									if (relativePosx >= div.offsetLeft && relativePosx < canvas.width) {
										resize = true;
										//segmentEndDelta += (relativePosx - div.offsetLeft) - div.offsetWidth;
										posX = div.offsetLeft;
										width = relativePosx - posX;
									}
								} else if (scope.waveData.resizingEdge == resizingEdgeLeft) {
									if (relativePosx >= 0 && relativePosx < div.offsetLeft + div.offsetWidth) {
										resize = true;
										//segmentStartDelta += relativePosx - div.offsetLeft;
										posX = relativePosx;
										width = div.offsetLeft + div.offsetWidth - relativePosx;
									}
								} else {
									debuglog('canvasMouseMove resizingEdge null. Time: ' + scope.Time);
								}
								if (resize) {
									var sequence = getElementAttribute(resizeElement, 'segmentId');
									resizeHandler = $timeout(function () {
										requestAnimationFrame(() => {
											resizeHandler = null;
											div.style.left = posX + 'px';
											checkSegmentTitleVisibility(div, width);
											div.style.width = width + 'px';
											debuglog('canvasMouseMove - div resize left: ' + posX + ' width: ' + width + ' resizingEdge: ' + scope.waveData.resizingEdge + ' Time: ' + scope.data.Time);
											emitResizeElementChanged(scope);
										});										
									}, 50);

								}
							}
							
						}
					}
				}

				function canvasMouseLeave(event) {
					
					var scope = this;
					debuglog('canvasMouseLeave: x:' + event.clientX + ' y: ' + event.clientY + ' resizingEdge: '
						+ scope.waveData.resizingEdge + ' segmentResizing: ' + scope.waveData.segmentResizing + ' Time: ' + scope.data.Time + ' canvasY: ' + getCanvasY());
					if (scope.waveData.segmentResizing) {
						if (event.clientX - getCanvasX() > canvas.width) {
							if (resizeElement) {
								//segmentEndDelta += canvas.width - resizeElement.offsetWidth;
								requestAnimationFrame(() => {
									resizeElement.style.width = (canvas.width - resizeElement.offsetLeft) + 'px';
									emitResizeElementChanged(scope);
								});
								//emit info about the last resized element  to parent
							}
						}
						if (event.clientX < getCanvasX()) {
							if (resizeElement) {
								//segmentStartDelta += -1 * resizeElement.offsetLeft;
								var posX, width;
								posX = 0;
								width = scope.waveData.resizingEdge == resizingEdgeRight ? 0 : resizeElement.offsetWidth; //resizeElement.offsetWidth + resizeElement.offsetLeft;
								if (resizeHandler) {
									$timeout.cancel(resizeHandler);
									resizeHandler = null;
								}

								requestAnimationFrame(() => {
									resizeElement.style.left = '0px';
									resizeElement.style.width = width + 'px';
									emitResizeElementChanged(scope);
									checkSegmentTitleVisibility(resizeElement, width);
								})
								
							}
						}
						if (event.clientY < getCanvasY() || event.clientY >= getCanvasY() + scope.height) {
							//left canvas
							if (resizeElement && resizeElement != canvas) {
								if (resizeHandler) {
									$timeout.cancel(resizeHandler);
									resizeHandler = null;
								}
								if (event.clientY < getCanvasY()) {
									//going up
									if (scope.waveData.resizingEdge == resizingEdgeRight) {
										debuglog('canvasMouseLeave leaving up - hiding resizeElement - resizingEdge: ' + scope.waveData.resizingEdge + '  Time: ' + scope.data.Time);
										resizeElement.style.display = 'none';
									}										
									else {
										debuglog('canvasMouseLeave leaving up - filling to the beginning - resizingEdge: ' + scope.waveData.resizingEdge + '  Time: ' + scope.data.Time);
										var prevDiv = findPreviousDiv(scope.layer, scope.waveData.segmentResizing);
										var posX = prevDiv != null ? prevDiv.offsetLeft + prevDiv.offsetWidth : 0;
										var width = scope.data.widthPx - posX;
										renderBlock(scope.layer, posX, width, scope.waveData.segmentResizing, findSegmentTitle(scope.layer,scope.waveData.segmentResizing));
										emitResizeElementChanged(scope);
									}
								}									
								else {
									//going down
									if (scope.waveData.resizingEdge == resizingEdgeRight) {
										debuglog('canvasMouseLeave leaving down - rendering div to the end: ' + resizeElement.offsetLeft + ' width: '
											+ resizeElement.offsetWidth + ' resizeEdge: ' + scope.waveData.resizingEdge + ' Time: ' + scope.data.Time);
										requestAnimationFrame(() => {
											resizeElement = renderBlock(scope.layer, resizeElement.offsetLeft,
												canvas.width - resizeElement.offsetLeft, scope.waveData.segmentResizing,
												findSegmentTitle(scope.layer, scope.waveData.segmentResizing));
											emitResizeElementChanged(scope);
										});
									} else {
										debuglog('canvasMouseLeave leaving down - hiding div: ' + resizeElement.offsetLeft + ' width: '
											+ resizeElement.offsetWidth + ' resizeEdge: ' + scope.waveData.resizingEdge + ' Time: ' + scope.data.Time);
										resizeElement.style.display = 'none';
									}
									
								}
							}
						}
					}					
				}

				function canvasMouseEnter(event) {
					var scope = this;
					debuglog('canvasMouseEnter: x:' + event.clientX + ' y: ' + event.clientY + ' resizingEdge: ' + scope.waveData.resizingEdge + ' Time: ' + scope.data.Time);
					/*if (scope.onMouseEnter)
						scope.onMouseEnter();*/
					if (scope.waveData.segmentResizing) {
						var rect = event.target.getBoundingClientRect();
						var elem = findElement(scope.layer, scope.waveData.segmentResizing);
						debuglog('canvasMouseEnter- coords: clientY: ' + event.clientY + ' CanvasTop:  ' + rect.top + ' Time: ' + scope.data.Time);
						if (scope.waveData.lastChunkIndex < scope.index) {
							//Entered from the top
							debuglog('canvasMouseEnter-from top: x:' + event.clientX + ' y: ' + event.clientY + ' Time: ' + scope.data.Time);
							var nextDiv = findNextDiv(scope.layer, scope.waveData.segmentResizing);
							if (scope.waveData.resizingEdge == resizingEdgeRight) {
								var width = nextDiv != null ? nextDiv.offsetLeft : event.clientX - getCanvasX();
								resizeElement = renderBlock(scope.layer, 0, width, scope.waveData.segmentResizing,
									findSegmentTitle(scope.layer, scope.waveData.segmentResizing));
							}								
							else {								
								if (elem != null) {
									var width = (elem == canvas ? canvas.width : elem.offsetLeft + elem.offsetWidth) - (event.clientX - getCanvasX());
									resizeElement = renderBlock(scope.layer, event.clientX - getCanvasX(), width, scope.waveData.segmentResizing,
										findSegmentTitle(scope.layer, scope.waveData.segmentResizing));
								}
							}
							emitResizeElementChanged(scope);
							if (scope.index - scope.waveData.lastChunkIndex > 1) {
								//Some chunks were skipped during move (no mouseleave/enter fired, because of mouse speed)
								if (scope.requestChunksRender)
									scope.requestChunksRender({
										data:
										{
											from: scope.waveData.lastChunkIndex + 1,
											to: scope.index - 1,
											sequence: scope.waveData.segmentResizing,
											resizingEdge: scope.waveData.resizingEdge
										}
									})
							}
						} else if (scope.waveData.lastChunkIndex > scope.index) {
							//Entered from bottom
							debuglog('canvasMouseEnter-from bottom: x:' + event.clientX + ' y: ' + event.clientY + ' resizingEdge: ' + scope.waveData.resizingEdge + ' Time: ' + scope.data.Time);							
							if (scope.waveData.resizingEdge == resizingEdgeRight) {
								/*resizeElement = scope.layer in segmentDivs ? segmentDivs[scope.layer].find(d => getElementAttribute(d, 'segmentId') == scope.waveData.segmentResizing) : null;
								if (resizeElement != null) {
									var width = event.clientX - getCanvasX() - resizeElement.offsetLeft;
									if (width >= 0) {
										debuglog('canvasMouseEnter - setting width: left:' + resizeElement.offsetLeft
											+ ' width: ' + resizeElement.offsetWidth + ' resizing edge: ' + scope.waveData.resizingEdge + ' Time: ' + scope.data.Time + ' new width: ' + width);
										resizeElement.style.width = width + 'px';
									}
									emitResizeElementChanged(scope);
								}*/
								var nextDiv = findNextDiv(scope.layer, scope.waveData.segmentResizing);
								var posX = elem != null ? (elem == canvas ? 0 : elem.offsetLeft) : 0;
								var rightPos = nextDiv != null ? nextDiv.offsetLeft : event.clientX - getCanvasX();
								resizeElement = renderBlock(scope.layer, posX, rightPos - posX, scope.waveData.segmentResizing,
									findSegmentTitle(scope.layer, scope.waveData.segmentResizing));
							} else {
								
								var prevDiv = findPreviousDiv(scope.layer, scope.waveData.segmentResizing);
								var leftPos = prevDiv != null ? prevDiv.offsetLeft + prevDiv.offsetWidth : event.clientX - getCanvasX();
								resizeElement = renderBlock(scope.layer, leftPos, (elem == canvas || elem == null ? canvas.width : elem.offsetLeft + elem.offsetWidth) - leftPos,
									scope.waveData.segmentResizing, findSegmentTitle(scope.layer, scope.waveData.segmentResizing));
							}
							if(resizeElement)
								emitResizeElementChanged(scope);
							if (scope.waveData.lastChunkIndex - scope.index > 1) {
								//Some chunks were skipped during move (no mouseleave/enter fired, because of mouse speed)
								if (scope.requestChunksRender)
									scope.requestChunksRender({
										data:
										{
											from: scope.index + 1,
											to: scope.waveData.lastChunkIndex - 1,
											sequence: scope.waveData.segmentResizing,
											resizingEdge: scope.waveData.resizingEdge
										}
									})
							}
													
						}						
						
					}
					scope.waveData.lastChunkIndex = scope.index;
				}

				function findNextDiv(layer, segmentid) {
					if (layer in segmentDivs && segmentDivs[layer]) {
						var segmentDiv = segmentDivs[layer].find(d => getElementAttribute(d, 'segmentid') == segmentid);
						if (segmentDiv != null)
							return segmentDivs[layer].find(d => getElementAttribute(d, 'segmentid') != segmentid && d.offsetLeft > segmentDiv.offsetLeft);
						else {
							if (segmentDivs[layer].length > 0)
								return segmentDivs[layer].sort((a, b) => a.offsetLeft < b.offsetLeft)[0];
						}
					}
					return null;
				}

				function findPreviousDiv(layer, segmentid) {
					if (layer in segmentDivs && segmentDivs[layer]) {
						var segmentDiv = findElement(layer, segmentid);
						if (segmentDiv != null) {
							return segmentDivs[layer].find(d => getElementAttribute(d, 'segmentid') != segmentid && d.offsetLeft < segmentDiv.offsetLeft);
						} else {
							if (segmentDivs[layer].length > 0)
								return segmentDivs[layer].sort((a, b) => a.offsetLeft > b.offsetLeft)[0];
						}
													
					}
					return null;
				}

				function findElement(layer, segmentid) {
					if ((layer in fullSegments && fullSegments[layer] == true) || getElementAttribute(canvas,'segmentid') == segmentid)
						return canvas;
					if (layer in segmentDivs && segmentDivs[layer] != null)
						return segmentDivs[layer].find(d => getElementAttribute(d, 'segmentid') == segmentid);
					return null;
				}

				function emitResizeElementChanged(scope) {
					if (scope.resizeElementChanged && resizeElement)
						scope.resizeElementChanged({ data: { start: resizeElement.offsetLeft, width: resizeElement.offsetWidth } });
				}

				function checkOnEdge(event, scope) {
					
					var relativePosx = event.clientX - getCanvasX();
					var result = { edge: null, element: null };
					
					var layer = scope.layer;					
					var full = layer in fullSegments && fullSegments[layer] == true;
					var segments = getSegments(scope.data, layer);
					//Check full
					if (full) {
						var segment = segments != null && segments.length > 0 ? segments[0] : null;
						var last = segment != null ? segment.last : false;
						var first = segment != null ? segment.first : false;
						
						if (last && canvas.width - relativePosx <= resizePosTolerance ||
							first && relativePosx <= resizePosTolerance) {
							result.edge = last ? resizingEdgeRight : resizingEdgeLeft;
							result.element = canvas;
							return result;
						}							
					} else if (layer in segmentDivs) {
						debuglog('checkOnEdge Time: ' + scope.data.Time);
						var div = segmentDivs[layer].find(d => relativePosx >= d.offsetLeft && relativePosx < d.offsetLeft + d.offsetWidth);
						if (div != null) {
							//var sequence = getElementAttribute(div, 'segmentId');
							//var segment = null;
							//if (segments != null) {
							//	segment = segments.find(s => s.sequence == sequence);
							//}
							var segment = getSegment(div, scope, layer);
							
							var last = segment != null ? segment.last : false;
							var first = segment != null ? segment.first : false;
							if (last && div.offsetLeft + div.offsetWidth - relativePosx <= resizePosTolerance) {
								result.element = div;
								debuglog('checkonEdge right Time: ' + scope.data.Time)
								result.edge = resizingEdgeRight;
							}
							if(first && (relativePosx - div.offsetLeft) <= resizePosTolerance) {								
								result.element = div;
								debuglog('checkonEdge left Time: ' + scope.data.Time)
								result.edge = resizingEdgeLeft;
							}
								
						}
					}
					return result;
				}

				//function findSegmentFromElement(scope, sequence) {
				//	var layer = scope.layer;
				//	var segments = getSegments(scope, layer);					
				//	if (layer in fullSegments && fullSegments[layer] == true) {
				//		return segments != null && segments.length > 0 ? segments[0] : null;
				//	} else {
				//		if (segments != null) {
				//			return segments.find(s => s.sequence == sequence);
				//		}
				//	}
				//	return null;
				//}

				function getSegments(data,layer) {
					return webPlayerFactory.getSegments(data, layer);
				}

				function getSegment(element, scope, layer) {
					var segments = getSegments(scope.data, layer);
					var sequence = getElementAttribute(element, 'segmentId');
					var segment = null;
					if (segments != null) {
						return segments.find(s => s.sequence == sequence);
					}
					return null;
				}

				/*$scope.$watch('segmentResizing',
					(newValue, oldValue, scope) => {
						if (newValue == null && oldValue != null) {
							//Handle ending segment outside of this control 
							if (resizeElement) {
								var sequence = getElementAttribute(resizeElement, 'segmentId');
								if (sequence == oldValue) {
									
									if (scope.onSegmentResized) {
										scope.onSegmentResized({
											data: {
												seq: sequence,
												start: resizeElement.offsetLeft,
												width: resizeElement.offsetWidth,
												edge: scope.resizingEdge
											}
										});
									}									
								}
							}
						}
					}
				);*/

				function getCanvasX() {
					if (canvasX == null)
						canvasX = canvas.getBoundingClientRect().left;
					return canvasX;
				}

				function getCanvasY() {
					//if (canvasY == null)
						canvasY = canvas.getBoundingClientRect().top;
					return canvasY;
				}

				function checkSegmentTitleVisibility(div, width) {
					var child = div.firstChild;
					if (child != null) {
						child.style.display = width > 60 ? 'block' : 'none';
					}
				}

				function debuglog(message) {
					if (debug && (debugFilter == null || message.match(debugFilter)))
						console.log(message);
				}

				function fillDiv(sequence, direction) {
					//fill canvas with div of given sequence looking for left or right limit (because of the other segment)
					var posX = 0, width = $scope.data.widthPx;
					if (direction == fillDirectionLeft) {
						var prevDiv = findPreviousDiv($scope.layer, sequence);
						if (prevDiv != null) {
							posX = prevDiv.offsetLeft + prevDiv.offsetWidth;
							width = $scope.data.widthPx - posX;
						}						
					} else {
						var nextDiv = findNextDiv($scope.layer, sequence);
						if (nextDiv != null) {
							width = nextDiv.offsetLeft - posX;
						}
					}
					renderBlock(scope.layer, posX, width, sequence, findSegmentTitle($scope.layer, sequence));

				}

				function createSegmentTextNode(title, layer) {
					//var textNode = document.createElement('div');
					//textNode.innerText = title;
					//textNode.className = 'wp_waveform_segment_title noselect layer' + layer;
					//textNode.onclick = onClick;
					//textNode.ondblclick = onDblclick;
					//return textNode;
				}

				function addDiv(div, layer) {
					element[0].appendChild(div);
					segmentDivs[layer].push(div);
				}

				function removeDiv(div, layer) {
					var index = layer in segmentDivs && segmentDivs[layer] ? segmentDivs[layer].findIndex(d => d == div) : -1;
					if (index >= 0)
						segmentDivs[layer].splice(index, 1);
					element[0].removeChild(div);
				}
								
				function onDblclick(event) {
					var data = { x: getCanvasRelativeX(event) };
					$scope.onDblclick({ data: data });
				}

				function findSegmentTitle(layer, sequence) {
					var segment = $scope.waveData.getSegment(layer, sequence);
					return segment != null ? segment.name : '';
				}

				function getCanvasRelativeX(event) {
					if (event.target.tagName == 'CANVAS') {
						return event.offsetX;
					}
					//clicked on title div
					var parent = event.target.parentElement;
					if (parent.tagName != "DIV") {
						//Title inside canvas, no segment div - full row segment
						return event.target.offsetLeft + event.offsetX;
					} else {
						//inside div
						return event.target.offsetLeft + event.offsetX + parent.offsetLeft;
					}
				}
				
            }
        };
    }]);

