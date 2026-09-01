import argparse
from lxml import etree
import six
from google.cloud import translate_v2
import re
import os
import time
import os.path
import glob
import json


def namespace(element):
    m = re.match(r'\{(.*)\}', element.tag)
    return m.group(1) if m else ''


def translate(source_file, language, language_code, subdir, service_account_file, replace,
              update_original, update_changed):
    if source_file.lower().endswith('.json'):
        return translate_json(source_file, language_code, subdir, service_account_file, replace, update_changed)
    translate_client = translate_v2.Client.from_service_account_json(service_account_file)

    source_tree = etree.parse(source_file)
    source_root = source_tree.getroot()

    ns_map = {'': namespace(source_root)}

    if not subdir:
        subdir = language_code.upper()

    file_base, ext = os.path.splitext(os.path.basename(source_file))
    target_file = os.path.join(os.path.dirname(source_file), subdir, f'{file_base}-{subdir}{ext}')

    if not (replace or update_original or update_changed) and os.path.isfile(target_file):
        print(f'Target file already exists, skipping {source_file}')
        return

    language_tag = source_root.find('tags', ns_map).find('tag', ns_map)
    language_tag.set('language', language)

    def non_empty_tag(s):
        text_attr = s.get('text')
        return text_attr and text_attr.strip()

    source_string_tags = list(filter(non_empty_tag, source_root.find('strings', ns_map).findall('string', ns_map)))
    strings = [tag.get('text') for tag in source_string_tags]

    if len(strings) == 0:
        print(f'No translatable strings found, skipping {source_file}')
        return

    regex = re.compile(r'\{(.*)\}')

    def esc(s):
        unique_matches = set(re.findall(regex, s))
        for idx, m in enumerate(unique_matches):
            s = s.replace('{' + m + '}', f'{{{idx}}}')
        return s, unique_matches

    def unesc(s, unique_matches):
        for idx, m in enumerate(unique_matches):
            s = s.replace(f'{{{idx}}}', '{' + m + '}').replace('\u00A0', '')
        return s

    escaped_strings = [esc(s) for s in strings]
    translatable_strings = [s for s, _ in escaped_strings]
    string_escapes = [e for _, e in escaped_strings]
    # placeholders = [regex.sub() for s in strings]

    print(f'Translating {len(translatable_strings)} strings \n'
          f'  from: {source_file}\n'
          f'  to: {language}\n'
          f'  output: {target_file}')

    def chunks(lst, n):
        """Yield successive n-sized chunks from lst."""
        for i in range(0, len(lst), n):
            yield lst[i:i + n]

    translations = []
    CHUNK_SIZE = 10
    INDENT = '  > '
    print(INDENT, end='')
    for idx, c in enumerate(chunks(translatable_strings, CHUNK_SIZE)):
        print(f'{int(CHUNK_SIZE * idx * 100 / len(translatable_strings))}%..', end='')
        tchunk = translate_client.translate(c, source_language='en', target_language=language_code)
        ct = [t["translatedText"] for t in tchunk]
        translations.extend(ct)
    print(f'100%')

    for tag, e, t, o in zip(source_string_tags, string_escapes, translations, strings):
        tag.set('text', unesc(t, e))
        tag.set('original', o)

    # If the file exists, and we didn't specify entirely replacing it, we will merge existing content, and warn on
    # changed content
    if not replace and os.path.isfile(target_file):
        target_tree = etree.parse(target_file)
        target_root = target_tree.getroot()
        target_strings_root = target_root.find('strings', ns_map)
        if target_strings_root is None:
            # Just save the translated source directly and return
            print(INDENT + f'Target file has no "strings" element, replacing entirely')
            etree.ElementTree(source_root).write(target_file, encoding="utf-8", xml_declaration=True, pretty_print=True)
            return
        # Do merging by id
        target_string_tags = list(filter(non_empty_tag, target_strings_root.findall('string', ns_map)))
        for idx, source_string_tag in enumerate(source_string_tags):
            source_id = source_string_tag.get('id')
            source_text = source_string_tag.get('text')
            source_original = source_text
            # source_string_tag.get('original')
            try:
                target_string_tag = next(t for t in target_string_tags if t.get('id') == source_id)
                target_original = target_string_tag.get('original')
                if target_original != source_original and (update_original or update_changed) or not target_original:
                    target_string_tag.set('original', source_original)
                    print(INDENT + f'Updated {source_id} original attribute')
                    print(INDENT + f'  from "{target_original}"')
                    print(INDENT + f'  to "{source_original}"')
                target_text = target_string_tag.get('text')
                if update_changed and target_text != source_text:
                    target_string_tag.set('text', source_text)
                    print(INDENT + f'Updated {source_id} text attribute')
                    print(INDENT + f'  from "{target_text}"')
                    print(INDENT + f'  to "{source_text}"')
            except StopIteration:
                target_string_tag = etree.SubElement(target_strings_root, 'string')
                target_string_tag.set('id', source_id)
                target_string_tag.set('text', source_text)
                target_string_tag.set('original', source_original)
                print(INDENT + f'Added {source_id}')
                print(INDENT + f'  original "{source_original}"')
                print(INDENT + f'  text "{source_text}"')
        # Fix indents
        etree.indent(target_root, space="    ")
        etree.ElementTree(target_root).write(target_file, encoding="utf-8", xml_declaration=True, pretty_print=True)
    else:
        os.makedirs(os.path.dirname(target_file), exist_ok=True)
        etree.ElementTree(source_root).write(target_file, encoding="utf-8", xml_declaration=True, pretty_print=True)


def _flatten_json(value, prefix=''):
    result = {}
    for key, item in value.items():
        path = f'{prefix}.{key}' if prefix else key
        if isinstance(item, dict):
            result.update(_flatten_json(item, path))
        elif isinstance(item, str):
            result[path] = item
    return result


def _inflate_json(values):
    result = {}
    for path, value in values.items():
        cursor = result
        parts = path.split('.')
        for part in parts[:-1]:
            cursor = cursor.setdefault(part, {})
        cursor[parts[-1]] = value
    return result


def translate_json(source_file, language_code, subdir, service_account_file, replace, update_changed):
    """Translate the Extension's keyed JSON catalog while retaining reviewed values.

    Output records include source text and generation metadata so changed English is
    detectable without using the translated value as an identifier.
    """
    client = translate_v2.Client.from_service_account_json(service_account_file)
    with open(source_file, encoding='utf-8') as source_stream:
        source = json.load(source_stream)
    source_values = _flatten_json(source.get('messages', source))
    locale = subdir or language_code
    target_file = os.path.join(os.path.dirname(source_file), f'{locale}.json')
    existing = {}
    if os.path.isfile(target_file) and not replace:
        with open(target_file, encoding='utf-8') as target_stream:
            existing = json.load(target_stream).get('messages', {})
    pending = []
    protected = []
    placeholder_regex = re.compile(r'\{[A-Za-z0-9_.-]+\}|![A-Za-z0-9_-]+|<[^>]+>')
    for key, text in source_values.items():
        record = existing.get(key)
        if record and record.get('source') == text and not update_changed:
            continue
        tokens = placeholder_regex.findall(text)
        escaped = text
        for index, token in enumerate(tokens):
            escaped = escaped.replace(token, f'__BLT_TOKEN_{index}__')
        pending.append((key, text, escaped))
        protected.append(tokens)
    for start in range(0, len(pending), 10):
        chunk = pending[start:start + 10]
        translated = client.translate([item[2] for item in chunk], source_language='en', target_language=language_code)
        for offset, ((key, source_text, _), translated_item) in enumerate(zip(chunk, translated)):
            value = translated_item['translatedText']
            tokens = protected[start + offset]
            for index, token in enumerate(tokens):
                value = value.replace(f'__BLT_TOKEN_{index}__', token)
            existing[key] = {'text': value, 'source': source_text, 'machineGenerated': True}
    os.makedirs(os.path.dirname(target_file), exist_ok=True)
    with open(target_file, 'w', encoding='utf-8') as target_stream:
        json.dump({'locale': locale, 'messages': dict(sorted(existing.items()))}, target_stream, ensure_ascii=False, indent=2)
        target_stream.write('\n')


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Translate Bannerlord XML or Twitch Extension JSON catalogs')
    parser.add_argument('glob_patterns', metavar='glob', type=str, nargs='+')
    parser.add_argument('--account', dest='service_account_file', action='store', required=True)
    parser.add_argument('--lang', dest='lang', action='store', required=True)
    parser.add_argument('--replace', dest='replace', action='store_true')
    parser.add_argument('--update-original-tag', dest='update_original_tag', action='store_true')
    parser.add_argument('--update-changed', dest='update_changed', action='store_true')
    parser.add_argument('--lang-code', dest='langcode', action='store', required=True)
    parser.add_argument('--subdir-override', dest='subdiroverride', action='store')
    args = parser.parse_args()
    unique_files = list(set(f for pattern in args.glob_patterns for f in glob.glob(pattern, recursive=True)))
    if not unique_files:
        print('No files found matching the provided globs!')
    for source in unique_files:
        translate(source, args.lang, args.langcode, args.subdiroverride, args.service_account_file, args.replace,
                  args.update_original_tag, args.update_changed)
