

function wp_TimelineController(element, $timeout, webPlayerFactory) {
	var ctrl = this;
	ctrl.thumbs = [];
	ctrl.lastSegmentTotalDuration = null;
	ctrl.segmentDivs = {};
	var debug = false; //true;
	var debugFilter = new RegExp(/(renderBlock)|(canvasMouseLeave leaving up)+/, 'g'); //null;
	var resizePosTolerance = 3;
	var resizeElement;
	var resizeHandler;
	
	ctrl.getThumbStyle = function (t, index) {
		var width = ctrl.options.thumbWidth;
		/*if (index == 0 && ctrl.getVideoStartOffset() > 0) {
			width += ctrl.getVideoStartOffset() * ctrl.getPixelPerSecond();
		}
		if (index == ctrl.thumbs.length - 1) {
			width += ctrl.getVideoEndOffset() * ctrl.getPixelPerSecond();
		}*/
		return {
			width: width + 'px',
			height: ctrl.options.thumbHeight + 'px',
			'margin-left': ctrl.options.margin + 'px',
			'margin-top': ctrl.options.margin + 'px',
			'margin-bottom': ctrl.options.margin + 'px',
			'background-image': 'url("' + t.src + '")',
			'background-position': t.position + 'px 0px',
			'background-size' : t.size
		};
	}

	ctrl.getSeparatorStyle = function (t, index) {
		return {
			width: '1px',
			'background-color': '#AAA',
			height: t.time.seconds() == 0 || index == 0 ? '75%' : '25%',
			position: 'absolute'
			//left: (index == 0 ? ctrl.getVideoStartOffset() * ctrl.getPixelPerSecond() : 0).toString() + 'px'			
		}
	}

	ctrl.getHeaderStyle = function (t, index) {
		return {
			position: 'absolute'
			//left: (index == 0 ? ctrl.getVideoStartOffset() * ctrl.getPixelPerSecond() + 2 : 2).toString() + 'px'
		}
	}

	ctrl.$onInit = function () {
		/*ctrl.thumbs = ctrl.buildBlocks(ctrl.from, ctrl.to, ctrl.videoStartTime, moment(ctrl.videoStartTime).add(ctrl.videoDuration, 'seconds'),
			ctrl.options);*/
	}

	ctrl.$onChanges = function (changesObj) {
		if ('currentTime' in changesObj) {
			if (changesObj['currentTime'] && !changesObj['currentTime'].isFirstChange())
				ctrl.checkScroll();
		}
		if ('videoStartTime' in changesObj && changesObj['videoStartTime'].currentValue) {
			ctrl.thumbs = ctrl.buildBlocks(ctrl.from, ctrl.to, ctrl.videoStartTime, moment(ctrl.videoStartTime).add(ctrl.videoDuration, 'seconds'),
				ctrl.options);
		}
	}

	ctrl.$doCheck = function () {
		var totalSegmentDuration = calculateTotalDuration(ctrl.segments);
		if (ctrl.lastSegmentTotalDuration != totalSegmentDuration) {
			ctrl.lastSegmentTotalDuration = totalSegmentDuration;
			//TODO: rerender the segments
			ctrl.renderSegments();
		}
	}

	ctrl.getCursorStyle = function () {
		var time = ctrl.currentTime - ctrl.getVideoStartOffset();
		if (time < 0)	
			time = 0;
		//var timeLineSize = moment(ctrl.videoStartTime).add(ctrl.videoDuration, 'seconds');
		var timeLineSize = moment(ctrl.to).diff(ctrl.from, 'seconds');
		if (time > timeLineSize)
			time = timeLineSize;
		var pos = Math.ceil(time * ctrl.getPixelPerSecond());
		if (time == timeLineSize)
			pos--;
		return {
			left: pos.toString() + 'px'
		};
	}

	ctrl.getPixelPerSecond = function () {
		return (ctrl.options.thumbWidth + ctrl.options.margin) / ctrl.options.thumbStep;
	}

	ctrl.getTotalPxWidth = function () {
		return moment(ctrl.to).diff(moment(ctrl.from), 'seconds', true) * ctrl.getPixelPerSecond();
	}

	ctrl.checkScroll = function () {
		var pos = Math.ceil(ctrl.currentTime * ctrl.getPixelPerSecond());
		var holderElem = element[0].firstChild;
		var newScrollVal;
		if(pos > holderElem.scrollLeft + holderElem.clientWidth) {
			newScrollVal = pos - holderElem.clientWidth / 2;
			if(newScrollVal > ctrl.getTotalPxWidth() - holderElem.clientWidth)
				newScrollVal = ctrl.getTotalPxWidth() - holderElem.clientWidth;
			holderElem.scrollLeft = newScrollVal;
		}
		if(pos < holderElem.scrollLeft) {
			newScrollVal = pos - holderElem.clientWidth;
			if (newScrollVal < 0)
				newScrollVal = 0;
			holderElem.scrollLeft = newScrollVal;
		}
	}

	ctrl.onTimelineClick = function (ev) {
		var holderElem = element[0].firstChild;
		ctrl.onClick({
			data: {
				time: (ev.layerX) / ctrl.getPixelPerSecond(),
				ctrlKey: ev.ctrlKey
			}
		});
	}

	ctrl.getVideoStartOffset = function () {
		return moment(ctrl.from).diff(ctrl.videoStartTime, 'seconds', true).toFixed(2);
	}

	ctrl.getVideoEndOffset = function () {
		return ctrl.videoDuration - moment(ctrl.to).diff(ctrl.videoStartTime, 'seconds', true).toFixed(2);
	}

	ctrl.renderSegments = function () {
		for (var l in ctrl.segments) {
			var divs = [];
			if (l in ctrl.segmentDivs) {
				divs = ctrl.segmentDivs[l];
			}
			var pos = 0;
			var blankIntervals = [];
			var segments = ctrl.segments[l];
			for (var i = 0; i < segments.length; i++) {
				var segment = segments[i];
				var segmentStartSec = segment.start.diff(ctrl.videoStartTime, 'seconds', true);
				var segmentDurationSec = segment.end.diff(segment.start, 'seconds', true);
				if (segmentStartSec > pos)
					blankIntervals.push({ start: pos * ctrl.getPixelPerSecond(), end: segmentStartSec * ctrl.getPixelPerSecond() });
				pos = segmentStartSec;
				var div = divs.find(d => webPlayerFactory.getElementAttribute(d, 'segmentId') == segment.sequence);
				if (div == null) {
					div = document.createElement('div');
					div.className = 'wp_video_timeline_segment wp_video_timeline_segment_l' + l;
					div.ondblclick = ctrl.onSegmentDivDblclick;
					webPlayerFactory.addElementAttribute(div, 'segmentId', segment.sequence);					
					webPlayerFactory.addOrUpdateTextNode(div, segment.name, l, null, ctrl.onSegmentDblclick);
					if (!(l in ctrl.segmentDivs)) {
						ctrl.segmentDivs[l] = [];
					}
					ctrl.addDiv(element[0].querySelector('#layer_' + l), div, l);
				}
				//var maxTime = moment(ctrl.videoStartTime).add(ctrl.videoDuration, 'seconds', true);
				var maxTime = moment(ctrl.to).diff(ctrl.from, 'seconds', true);
				div.style.left = Math.floor(segmentStartSec * ctrl.getPixelPerSecond()) + 'px';
				div.style.width = Math.floor(Math.min(segmentDurationSec, maxTime) * ctrl.getPixelPerSecond()) + 'px';
				pos += segmentDurationSec;
			}
			if (pos < ctrl.videoDuration) {
				blankIntervals.push({ start: pos * ctrl.getPixelPerSecond(), end: ctrl.videoDuration * ctrl.getPixelPerSecond() });
			}
			//Remove unused divs
			var toRemove = [];
			for (var i = 0; i < ctrl.segmentDivs[l].length; i++) {
				var div = ctrl.segmentDivs[l][i];
				var match = blankIntervals.find(i => div.offsetLeft >= i.start && div.offsetLeft + div.offsetWidth <= i.end);
				if (match) {
					toRemove.push(i);
				}
			}
			for (var i = 0; i < toRemove.length; i++) {
				var ix = toRemove[i];
				var div = ctrl.segmentDivs[l][ix];
				ctrl.debuglog('remove div left: ' + div.offsetLeft + ' width: ' + div.offsetWidth);
				element[0].querySelector('#layer_' + l).removeChild(div);
				ctrl.segmentDivs[l][ix] = null;
			}
			if (ctrl.segmentDivs[l].length > 0)
				ctrl.segmentDivs[l] = ctrl.segmentDivs[l].filter(d => d != null);

		}
	}

	ctrl.addDiv = function(parent, div, layer) {
		parent.appendChild(div);
		ctrl.segmentDivs[layer].push(div);
	}

	ctrl.onSegmentDivDblclick = function (event) {
		ctrl.onSegmentDblclick({
			layer: webPlayerFactory.getElementAttribute(event.target.parentNode, 'layer'),
			time: (element[0].firstChild.scrollLeft + event.target.offsetLeft + event.layerX) / (ctrl.options.thumbWidth + ctrl.options.margin) * ctrl.options.thumbStep
		});		
	}

	ctrl.layerClick = function (event, layer) {
		ctrl.onLayerClick({
			data: {
				layer: layer,
				time: (element[0].firstChild.scrollLeft + event.target.offsetLeft + event.layerX) / ctrl.getPixelPerSecond(),
				ctrlKey: event.ctrlKey
			}
		});
	}

	ctrl.layerMouseMove = function (event, layer) {
		if (!ctrl.waveData.segmentResizing) {
			if (webPlayerFactory.getSegments(ctrl, layer)) {
				var result = ctrl.checkOnEdge(event, layer);	//return { bool, element}
				if (result.edge) {
					resizeElement = result.element;
					ctrl.debuglog('layerMouseMove - on edge. Resizing edge: ' + result.edge);
					//scope.resizingEdge = result.edge;
					webPlayerFactory.addClass(null, 'wp_resizing', event.target);
				} else {
					resizeElement = null;

					webPlayerFactory.removeClass(null, 'wp_resizing', event.target);
				}
			}
		} else {
			//resize active
			var posX, width;
			if (!resizeHandler) {
				var relativePosx = event.layerX;
				var div = resizeElement;
				if (event.target != event.currentTarget)
					relativePosx += div.offsetLeft;
				
				var layerDiv = div.parentElement;
				var resize = false;
				if (div != null) {
					if (ctrl.waveData.resizingEdge == webPlayerFactory.resizingEdgeRight) {
						if (relativePosx >= div.offsetLeft && relativePosx < layerDiv.clientWidth) {
							resize = true;
							//segmentEndDelta += (relativePosx - div.offsetLeft) - div.offsetWidth;
							posX = div.offsetLeft;
							width = relativePosx - posX;
						}
					} else if (ctrl.waveData.resizingEdge == webPlayerFactory.resizingEdgeLeft) {
						if (relativePosx >= 0 && relativePosx < div.offsetLeft + div.offsetWidth) {
							resize = true;
							//segmentStartDelta += relativePosx - div.offsetLeft;
							posX = relativePosx;
							width = div.offsetLeft + div.offsetWidth - relativePosx;
						}
					} else {
						ctrl.debuglog('layerMouseMove resizingEdge null. ');
					}
					if (resize) {
						var sequence = webPlayerFactory.getElementAttribute(resizeElement, 'segmentId');
						resizeHandler = $timeout(function () {
							requestAnimationFrame(() => {
								resizeHandler = null;
								div.style.left = posX + 'px';
								checkSegmentTitleVisibility(div, width);
								div.style.width = width + 'px';
								ctrl.debuglog('layerMouseMove - div resize left: ' + posX + ' width: ' + width + ' resizingEdge: ' + ctrl.waveData.resizingEdge);
							});
						}, 50);

					}
				}
			}
			
		}
	}

	ctrl.layerMouseDown = function (event, layer) {
		
		ctrl.debuglog('layerMouseDown x: ' + event.layerX + ' y: ' + event.layerY + ' hasResizeElement: ' + (resizeElement != null));
		if (resizeElement) {
			event.stopPropagation();
			var sequence = webPlayerFactory.getElementAttribute(resizeElement, 'segmentId');
			ctrl.waveData.segmentResizing = sequence;
			if (ctrl.onSegmentStartResizing) {
				ctrl.onSegmentStartResizing({
					data: {
						sequence: sequence,
						posX: event.layerX,
						resizingEdge: ctrl.checkOnEdge(event, layer).edge,
						elementX: resizeElement.offsetLeft,
						elementWidth: resizeElement.offsetWidth
					}
				});
				ctrl.debuglog('layerMouseDown: x: ' + event.layerX + ' y: ' + event.layerY + ' resizingEdge: ' + ctrl.waveData.resizingEdge);
			}
			
		}
	}

	ctrl.layerMouseUp = function (event, layer) {
		if (ctrl.waveData.segmentResizing) {
			ctrl.debuglog('layerMouseup x: ' + event.clientX + ' y: ' + event.clientY + ' sequence: ' + ctrl.waveData.segmentResizing);
			event.stopPropagation();
			if (resizeElement) {
				if (ctrl.onSegmentResized) {
					ctrl.onSegmentResized({
						data: {
							seq: ctrl.waveData.segmentResizing,
							startTime: resizeElement.offsetLeft / ctrl.getPixelPerSecond(),
							duration: resizeElement.offsetWidth / ctrl.getPixelPerSecond(),
							edge: ctrl.waveData.resizingEdge,
							layer: layer
						}
					});
				}
			}

		}
	}

	ctrl.getLayerStyle = function () {
		return {
			width: (ctrl.thumbs.length * (ctrl.options.thumbWidth + ctrl.options.margin) +
				(ctrl.getVideoStartOffset() + ctrl.getVideoEndOffset()) * ctrl.getPixelPerSecond()).toString() + 'px',
			zIndex: 1002
		};
	}

	ctrl.checkOnEdge = function (event, layer) {

		var relativePosx = event.offsetX;
		if (event.target != event.currentTarget) {
			//target is segment div
			relativePosx += event.target.offsetLeft;
		}
		var result = { edge: null, element: null };
		//var layer = webPlayerFactory.getElementAttribute(event.target.parentNode, 'layer');
		
		if (layer in ctrl.segmentDivs) {
			//debuglog('checkOnEdge Time: ' + scope.data.Time);
			var div = ctrl.segmentDivs[layer].find(d => relativePosx >= d.offsetLeft && relativePosx < d.offsetLeft + d.offsetWidth);
			if (div != null) {
				var segment = webPlayerFactory.getSegment(div, ctrl, layer);				
				if (div.offsetLeft + div.offsetWidth - relativePosx <= resizePosTolerance) {
					result.element = div;
					ctrl.debuglog('checkonEdge right Layer: ' + layer)
					result.edge = webPlayerFactory.resizingEdgeRight;
				}
				if ((relativePosx - div.offsetLeft) <= resizePosTolerance) {
					result.element = div;
					ctrl.debuglog('checkonEdge left layer: ' + layer)
					result.edge = webPlayerFactory.resizingEdgeLeft;
				}
			}
		}
		return result;
	}

	ctrl.buildBlocks = function (timelineFrom, timelineTo, videoFrom, videoTo, options) {
		var result = [];
		var time = moment(timelineFrom);
		var mTo = moment(timelineTo);
		var first = true;

		while (time.isBefore(mTo)) {
			var offset = (time.minutes()*60 + time.seconds()) % options.blockTime;
			var imageFrom = moment(time).add(-1 * offset, 'seconds');
			/*var last = mTo.diff(time, 'seconds', true) < options.thumbStep;
			var size = first ? Math.ceil((1 + ctrl.getVideoStartOffset()/options.thumbStep) * options.blockTime * ctrl.getPixelPerSecond()) + 'px ' + options.thumbHeight + 'px' :
				last ? Math.ceil((1 + ctrl.getVideoEndOffset() / options.thumbStep) * options.blockTime * ctrl.getPixelPerSecond()) + 'px ' + options.thumbHeight + 'px' : 'initial';*/
			var size = 'initial';
			var thumb = {
				src: `${options.thumbUrl}/GetThumbnails.ashx?date=${imageFrom.format('YYYY-MM-DD')}&start=${imageFrom.format('HH:mm:ss')}&end=${moment(imageFrom).add(options.blockTime, 'seconds').format('HH:mm:ss')}&w=${options.thumbWidth}&h=${options.thumbHeight}`,				
				//time: first ? moment(videoFrom) : moment(time),
				time: moment(time),
				headerText: time.seconds() == 0 ? time.format('HH:mm') : '',
				position: -1 * (offset / options.thumbStep * options.thumbWidth),
				size: size
			};
			result.push(thumb);
			first = false;
			time.add(options.thumbStep, 'seconds');
			/*if(last)
				time = mTo;*/
		}
		return result;
	}

	ctrl.debuglog = function(message) {
		if (debug && (debugFilter == null || message.match(debugFilter)))
			console.log(message);
	}
}




function calculateTotalDuration(dictSegments) {
	var result = 0;
	for (var layer in dictSegments) {
		for (var i = 0; i < dictSegments[layer].length; i++) {
			var segment = dictSegments[layer][i];
			result += segment.end.diff(segment.start, 'seconds', true);
		}
	}
	return result;
}

function checkSegmentTitleVisibility(div, width) {
	var child = div.firstChild;
	if (child != null) {
		child.style.display = width > 60 ? 'block' : 'none';
	}
}



angular.module('app')
	.component('wpTimeline',
		{
			templateUrl: '/Modules/WebPlayer/timeline.html',
			controller: ['$element','$timeout', 'webPlayerFactory', wp_TimelineController],
			bindings: {
				from: '<',
				to: '<',
				currentTime: '<',
				options: '<',				
				onClick: '&',
				onDblClick: '&',
				segmentLayers: '<',
				segments: '<',
				onSegmentDblclick: '&',
				waveData: '<',
				videoStartTime: '<',
				videoDuration: '<',
				onLayerClick: '&',
				onSegmentStartResizing: '&',
				onSegmentResized: '&'
			}
		});

//wp_TimelineController.$inject = ['$element','$timeout','webPlayerFactory'];
