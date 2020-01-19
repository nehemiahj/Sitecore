function toggleChkBoxMethod2(formName){
	var form=$(formName);
	var i=form.getElements('checkbox'); 
	i.each(function(item){
		item.checked = !item.checked;
	});
	$('togglechkbox').checked = !$('togglechkbox').checked;
}