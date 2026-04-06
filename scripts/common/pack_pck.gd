extends SceneTree

func _init() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() < 2:
		printerr("Usage: pack_pck.gd <out.pck> <mod.json> [mod_image.png] [mod_image.png.import imported.ctex ctex_target]")
		quit(2)
		return

	var out_pck := args[0]
	var manifest_src := args[1]
	var image_src := ""
	var image_import_src := ""
	var image_ctex_src := ""
	var image_ctex_target := ""
	if args.size() >= 3:
		image_src = args[2]
	if args.size() >= 6:
		image_import_src = args[3]
		image_ctex_src = args[4]
		image_ctex_target = args[5]

	if not FileAccess.file_exists(manifest_src):
		printerr("Missing manifest file: ", manifest_src)
		quit(3)
		return

	var mod_id := ""
	var manifest_json := FileAccess.get_file_as_string(manifest_src)
	var manifest_data = JSON.parse_string(manifest_json)
	if manifest_data is Dictionary:
		if manifest_data.has("id"):
			mod_id = str(manifest_data["id"]).strip_edges()
		elif manifest_data.has("pck_name"):
			mod_id = str(manifest_data["pck_name"]).strip_edges()
	if mod_id.is_empty():
		mod_id = out_pck.get_file().get_basename()

	var packer := PCKPacker.new()
	var err := packer.pck_start(out_pck)
	if err != OK:
		printerr("pck_start failed with code: ", err)
		quit(10)
		return

	if image_src != "" and FileAccess.file_exists(image_src):
		err = packer.add_file("res://mod_image.png", image_src)
		if err != OK:
			printerr("Failed to add mod_image.png, code: ", err)
			quit(12)
			return

		err = packer.add_file("res://%s/mod_image.png" % mod_id, image_src)
		if err != OK:
			printerr("Failed to add namespaced mod_image.png, code: ", err)
			quit(14)
			return

		if image_import_src != "" and FileAccess.file_exists(image_import_src):
			err = packer.add_file("res://%s/mod_image.png.import" % mod_id, image_import_src)
			if err != OK:
				printerr("Failed to add namespaced mod_image.png.import, code: ", err)
				quit(15)
				return

		if image_ctex_src != "" and FileAccess.file_exists(image_ctex_src) and image_ctex_target != "":
			var ctex_path := image_ctex_target
			if not ctex_path.begins_with("res://"):
				ctex_path = "res://" + ctex_path
			err = packer.add_file(ctex_path, image_ctex_src)
			if err != OK:
				printerr("Failed to add imported ctex, code: ", err)
				quit(16)
				return

	err = packer.flush()
	if err != OK:
		printerr("flush() failed with code: ", err)
		quit(13)
		return

	print("Packed: ", out_pck)
	quit(0)
