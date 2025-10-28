
import { useState } from 'react';
import { View, Text, TextInput, Button, Image, Alert, ScrollView, ActivityIndicator } from 'react-native';
import * as ImagePicker from 'expo-image-picker';
import { supabase } from './supabase';

export default function App() {
  const [nombre, setNombre] = useState('');
  const [edad, setEdad] = useState('');
  const [comentarios, setComentarios] = useState('');
  const [image, setImage] = useState(null); // { uri } | null
  const [submitting, setSubmitting] = useState(false);

  const pickImage = async () => {
    const { status } = await ImagePicker.requestCameraPermissionsAsync();
    if (status !== 'granted') {
      Alert.alert('Permiso', 'Se requiere permiso de cámara.');
      return;
    }
    const result = await ImagePicker.launchCameraAsync({
      allowsEditing: true,
      quality: 0.7,
    });
    if (!result.canceled) {
      setImage(result.assets[0]);
    }
  };

  async function uploadImageAndInsertRow() {
    try {
      setSubmitting(true);

      let uploadedPath = null;
      let publicUrl = null;

      if (image?.uri) {
        // Convertir la imagen a blob
        const response = await fetch(image.uri);
        const blob = await response.blob();

        const ext = image.uri.split('.').pop() || 'jpg';
        const filename = `photo_${Date.now()}_${Math.floor(Math.random()*1e6)}.${ext}`;
        const filePath = `${filename}`; // guardamos en raíz del bucket

        const { error: uploadError } = await supabase.storage
          .from('photos')
          .upload(filePath, blob, { upsert: false, contentType: blob.type || 'image/jpeg' });
        if (uploadError) throw uploadError;

        uploadedPath = filePath;
        const { data } = supabase.storage.from('photos').getPublicUrl(filePath);
        publicUrl = data.publicUrl;
      }

      const edadInt = edad ? parseInt(edad, 10) : null;

      const { error: insertError } = await supabase
        .from('survey_responses')
        .insert({
          nombre: nombre || null,
          edad: Number.isNaN(edadInt) ? null : edadInt,
          comentarios: comentarios || null,
          photo_path: uploadedPath,
          photo_url: publicUrl,
        });

      if (insertError) throw insertError;

      Alert.alert('Éxito', 'Encuesta enviada correctamente.');
      setNombre('');
      setEdad('');
      setComentarios('');
      setImage(null);
    } catch (e) {
      console.error(e);
      Alert.alert('Error', e.message ?? 'Ocurrió un error');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <ScrollView contentContainerStyle={{ padding: 20, gap: 12 }}>
      <Text style={{ fontSize: 22, fontWeight: 'bold' }}>Encuesta</Text>

      <Text>Nombre</Text>
      <TextInput
        value={nombre}
        onChangeText={setNombre}
        placeholder="Tu nombre"
        style={{ borderWidth: 1, borderColor: '#ccc', padding: 10, borderRadius: 8 }}
      />

      <Text>Edad</Text>
      <TextInput
        value={edad}
        onChangeText={setEdad}
        placeholder="Ej: 30"
        keyboardType="numeric"
        style={{ borderWidth: 1, borderColor: '#ccc', padding: 10, borderRadius: 8 }}
      />

      <Text>Comentarios</Text>
      <TextInput
        value={comentarios}
        onChangeText={setComentarios}
        placeholder="Escribe tus comentarios"
        multiline
        numberOfLines={4}
        style={{ borderWidth: 1, borderColor: '#ccc', padding: 10, borderRadius: 8, textAlignVertical: 'top' }}
      />

      {image && (
        <View style={{ alignItems: 'center' }}>
          <Image source={{ uri: image.uri }} style={{ width: 240, height: 240, borderRadius: 12 }} />
        </View>
      )}

      <Button title="Tomar foto" onPress={pickImage} />

      <View style={{ height: 10 }} />
      {submitting ? (
        <ActivityIndicator size="large" />
      ) : (
        <Button title="Enviar" onPress={uploadImageAndInsertRow} />
      )}
    </ScrollView>
  );
}
