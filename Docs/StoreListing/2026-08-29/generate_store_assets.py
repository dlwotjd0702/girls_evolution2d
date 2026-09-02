from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
CAPTURES = OUT / "captures"
PHONE = OUT / "phone"
PHONE.mkdir(parents=True, exist_ok=True)

FONT = ROOT / "Assets/Resources/Font/Noto_Sans_KR/static/NotoSansKR-Regular.ttf"
DISPLAY_FONT = ROOT / "Assets/Fonts/army Bold.ttf"


def font(size: int, display: bool = False) -> ImageFont.FreeTypeFont:
    candidate = DISPLAY_FONT if display else FONT
    try:
        return ImageFont.truetype(str(candidate), size=size)
    except OSError:
        return ImageFont.truetype(str(FONT), size=size)


def cover(img: Image.Image, size: tuple[int, int]) -> Image.Image:
    img = img.convert("RGBA")
    scale = max(size[0] / img.width, size[1] / img.height)
    resized = img.resize((round(img.width * scale), round(img.height * scale)), Image.Resampling.LANCZOS)
    left = (resized.width - size[0]) // 2
    top = (resized.height - size[1]) // 2
    return resized.crop((left, top, left + size[0], top + size[1]))


def crop_game(path: Path) -> Image.Image:
    # Unity Game view at 0.47x. This removes the editor chrome at an exact 9:16 ratio.
    img = Image.open(path).convert("RGB")
    game = img.crop((709, 121, 1213, 1017))
    return game.resize((1080, 1920), Image.Resampling.LANCZOS).convert("RGBA")


def alpha_crop(path: Path) -> Image.Image:
    img = Image.open(path).convert("RGBA")
    bbox = img.getchannel("A").getbbox()
    return img.crop(bbox) if bbox else img


def paste_fit(base: Image.Image, sprite: Image.Image, box: tuple[int, int, int, int], anchor: str = "center") -> None:
    left, top, right, bottom = box
    max_w, max_h = right - left, bottom - top
    scale = min(max_w / sprite.width, max_h / sprite.height)
    resized = sprite.resize((max(1, round(sprite.width * scale)), max(1, round(sprite.height * scale))), Image.Resampling.LANCZOS)
    x = left + (max_w - resized.width) // 2
    y = top if anchor == "top" else top + (max_h - resized.height) // 2
    base.alpha_composite(resized, (x, y))


def rounded_label(base: Image.Image, box: tuple[int, int, int, int], title: str, subtitle: str | None = None) -> None:
    layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    draw.rounded_rectangle(box, radius=34, fill=(20, 7, 37, 232), outline=(235, 181, 72, 255), width=6)
    cx = (box[0] + box[2]) // 2
    ty = box[1] + 23
    draw.text((cx, ty), title, font=font(56, True), anchor="ma", fill=(255, 246, 216), stroke_width=3, stroke_fill=(70, 28, 74))
    if subtitle:
        draw.text((cx, ty + 70), subtitle, font=font(28), anchor="ma", fill=(244, 206, 118))
    base.alpha_composite(layer)


def add_glow(base: Image.Image, xy: tuple[int, int], radius: int, color=(253, 189, 66, 170)) -> None:
    glow = Image.new("RGBA", base.size, (0, 0, 0, 0))
    g = ImageDraw.Draw(glow)
    x, y = xy
    g.ellipse((x - radius, y - radius, x + radius, y + radius), fill=color)
    glow = glow.filter(ImageFilter.GaussianBlur(radius // 2))
    base.alpha_composite(glow)


def sd(level: int) -> Image.Image:
    return alpha_crop(ROOT / f"Assets/닌자SD2/{level}.png")


def make_icon() -> Path:
    bg = cover(Image.open(ROOT / "Assets/Art/Fields/astral-v2.png"), (512, 512))
    shade = Image.new("RGBA", bg.size, (25, 5, 48, 84))
    bg.alpha_composite(shade)
    add_glow(bg, (280, 245), 210, (184, 77, 255, 155))

    overlay = Image.new("RGBA", bg.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(overlay)
    d.ellipse((18, 18, 494, 494), outline=(247, 198, 83, 220), width=12)
    d.ellipse((35, 35, 477, 477), outline=(107, 42, 142, 210), width=7)
    bg.alpha_composite(overlay)

    hero = alpha_crop(ROOT / "Assets/Art/Characters/Skins/lv24-d-LD.png")
    # Upper-body portrait keeps facial features legible under the Play Store mask.
    crop_h = min(hero.height, round(hero.width * 1.1))
    hero = hero.crop((0, 0, hero.width, crop_h))
    paste_fit(bg, hero, (-40, -30, 575, 600), anchor="top")

    path = OUT / "app-icon-512.png"
    bg.convert("RGB").save(path, "PNG", optimize=True)
    return path


def make_feature_graphic() -> Path:
    bg = cover(Image.open(ROOT / "Assets/Art/Fields/astral-v2.png"), (1024, 500))
    bg = bg.filter(ImageFilter.GaussianBlur(1.2))
    tint = Image.new("RGBA", bg.size, (26, 5, 49, 115))
    bg.alpha_composite(tint)

    left = alpha_crop(ROOT / "Assets/Art/Characters/Mythic/phoenix.png")
    right = alpha_crop(ROOT / "Assets/Art/Characters/Skins/lv24-d-LD.png")
    paste_fit(bg, left, (-70, -8, 390, 650), anchor="top")
    paste_fit(bg, right, (695, 0, 1085, 610), anchor="top")

    text_layer = Image.new("RGBA", bg.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(text_layer)
    d.rounded_rectangle((300, 105, 760, 370), radius=38, fill=(20, 6, 39, 212), outline=(235, 180, 68, 245), width=6)
    d.text((530, 165), "미소녀닌자", font=font(68, True), anchor="mm", fill=(255, 246, 214), stroke_width=5, stroke_fill=(72, 27, 79))
    d.text((530, 246), "합성", font=font(92, True), anchor="mm", fill=(255, 207, 78), stroke_width=6, stroke_fill=(76, 25, 71))
    d.text((530, 325), "합성 · 수집 · 환생", font=font(30), anchor="mm", fill=(255, 241, 202))
    bg.alpha_composite(text_layer)

    path = OUT / "feature-graphic-1024x500.png"
    bg.convert("RGB").save(path, "PNG", optimize=True)
    return path


def make_merge_shot() -> Path:
    base = crop_game(CAPTURES / "01-main-ui.png")
    rounded_label(base, (72, 455, 1008, 610), "같은 닌자를 합쳐 진화!", "간단한 드래그로 더 높은 단계의 닌자를 발견하세요")

    add_glow(base, (270, 890), 155, (154, 78, 232, 120))
    add_glow(base, (810, 890), 155, (154, 78, 232, 120))
    add_glow(base, (540, 1270), 190, (253, 187, 67, 135))
    paste_fit(base, sd(1), (105, 690, 435, 1040))
    paste_fit(base, sd(1), (645, 690, 975, 1040))
    paste_fit(base, sd(2), (365, 1150, 715, 1570))

    d = ImageDraw.Draw(base)
    d.text((540, 900), "+", font=font(118, True), anchor="mm", fill=(255, 222, 109), stroke_width=5, stroke_fill=(68, 23, 70))
    d.polygon([(500, 1010), (580, 1010), (580, 1050), (630, 1050), (540, 1120), (450, 1050), (500, 1050)], fill=(255, 218, 91), outline=(86, 28, 77))

    path = PHONE / "01-merge-evolve-1080x1920.png"
    base.convert("RGB").save(path, "PNG", optimize=True)
    return path


def make_fever_shot() -> Path:
    base = crop_game(CAPTURES / "01-main-ui.png")
    draw = ImageDraw.Draw(base)
    draw.rounded_rectangle((300, 340, 858, 382), radius=20, fill=(230, 157, 45), outline=(255, 221, 111), width=3)
    draw.rounded_rectangle((67, 300, 272, 390), radius=14, fill=(29, 8, 48))
    draw.text((170, 347), "24/30", font=font(43), anchor="mm", fill=(255, 244, 219))
    rounded_label(base, (90, 520, 990, 675), "연속 터치로 FEVER!", "피버 게이지와 자동 소환·합성으로 성장 속도를 높이세요")

    for level, box in [
        (1, (70, 760, 350, 1080)),
        (6, (370, 760, 700, 1110)),
        (14, (720, 760, 1010, 1110)),
        (20, (360, 1160, 720, 1550)),
    ]:
        add_glow(base, ((box[0] + box[2]) // 2, (box[1] + box[3]) // 2), 125, (140, 67, 220, 95))
        paste_fit(base, sd(level), box)

    hi = Image.new("RGBA", base.size, (0, 0, 0, 0))
    hd = ImageDraw.Draw(hi)
    hd.rounded_rectangle((196, 1680, 444, 1905), radius=26, outline=(255, 219, 92, 245), width=10)
    hd.rounded_rectangle((638, 1680, 890, 1905), radius=26, outline=(255, 219, 92, 245), width=10)
    base.alpha_composite(hi.filter(ImageFilter.GaussianBlur(1)))

    path = PHONE / "02-fever-automation-1080x1920.png"
    base.convert("RGB").save(path, "PNG", optimize=True)
    return path


def make_skin_shot() -> Path:
    base = crop_game(CAPTURES / "02-codex-skins.png")
    # Replace the editor-only preview note with the real player-facing collection value.
    layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.rounded_rectangle((95, 470, 985, 700), radius=24, fill=(21, 7, 37, 250), outline=(224, 168, 66, 245), width=5)
    d.text((540, 515), "96종 스킨 · 수집 효과", font=font(45, True), anchor="ma", fill=(255, 224, 120))
    d.text((540, 580), "수익 · 클릭 · 합성 속도 · 생성 속도", font=font(29), anchor="ma", fill=(255, 245, 218))
    d.text((540, 635), "해금 즉시 영구 적용", font=font(28), anchor="ma", fill=(224, 202, 255))
    base.alpha_composite(layer)
    path = PHONE / "03-skins-collection-1080x1920.png"
    base.convert("RGB").save(path, "PNG", optimize=True)
    return path


def make_plain_shot(source: str, output: str) -> Path:
    base = crop_game(CAPTURES / source)
    path = PHONE / output
    base.convert("RGB").save(path, "PNG", optimize=True)
    return path


def make_previews(icon_path: Path, feature_path: Path, phone_paths: list[Path]) -> list[Path]:
    sheet = Image.new("RGB", (1260, 1040), (22, 17, 29))
    draw = ImageDraw.Draw(sheet)
    icon = Image.open(icon_path).convert("RGB").resize((250, 250), Image.Resampling.LANCZOS)
    feature = Image.open(feature_path).convert("RGB").resize((922, 450), Image.Resampling.LANCZOS)
    sheet.paste(icon, (30, 35))
    sheet.paste(feature, (308, 35))
    draw.text((30, 320), "Google Play 등록정보 초안", font=font(42, True), fill=(255, 220, 111))
    draw.text((30, 380), "아이콘 · 대표 그래픽 · 휴대전화 스크린샷 7장", font=font(25), fill=(238, 230, 213))

    thumb_w, thumb_h = 160, 284
    for i, path in enumerate(phone_paths):
        thumb = Image.open(path).convert("RGB").resize((thumb_w, thumb_h), Image.Resampling.LANCZOS)
        x = 30 + i * 170
        sheet.paste(thumb, (x, 620))
        draw.text((x + thumb_w // 2, 590), f"{i + 1}", font=font(26), anchor="mm", fill=(255, 218, 100))
    overview = OUT / "preview-overview.jpg"
    sheet.save(overview, "JPEG", quality=91, optimize=True)

    mobile = Image.new("RGB", (840, 1110), (17, 13, 23))
    md = ImageDraw.Draw(mobile)
    md.text((420, 35), "휴대전화 축소 가독성 검수", font=font(38, True), anchor="ma", fill=(255, 220, 111))
    for i, path in enumerate(phone_paths):
        thumb = Image.open(path).convert("RGB").resize((180, 320), Image.Resampling.LANCZOS)
        col, row = i % 4, i // 4
        x, y = 30 + col * 200, 130 + row * 455
        mobile.paste(thumb, (x, y))
        md.text((x + 90, y + 348), path.stem.split("-1080")[0], font=font(16), anchor="ma", fill=(235, 229, 214))
    mobile_path = OUT / "preview-mobile.jpg"
    mobile.save(mobile_path, "JPEG", quality=91, optimize=True)
    return [overview, mobile_path]


def main() -> None:
    icon_path = make_icon()
    feature_path = make_feature_graphic()
    phone_paths = [
        make_merge_shot(),
        make_fever_shot(),
        make_skin_shot(),
        make_plain_shot("07-codex-detail-ld.png", "04-ld-detail-1080x1920.png"),
        make_plain_shot("06-prestige.png", "05-prestige-1080x1920.png"),
        make_plain_shot("04-summon.png", "06-summon-1080x1920.png"),
        make_plain_shot("05-offline-reward.png", "07-offline-reward-1080x1920.png"),
    ]
    assets = [icon_path, feature_path, *phone_paths, *make_previews(icon_path, feature_path, phone_paths)]
    report = []
    for path in assets:
        img = Image.open(path)
        report.append({"file": str(path.relative_to(ROOT)), "size": img.size, "mode": img.mode, "bytes": path.stat().st_size})
    (OUT / "asset-manifest.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
